using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.Linq.Extensions;
using Abp.Localization;
using Abp.Runtime.Session;
using Abp.UI;
using AdvantageControl.Authorization;
using AdvantageControl.Authorization.Accounts;
using AdvantageControl.Authorization.Roles;
using AdvantageControl.Authorization.Users;
using AdvantageControl.Customers;
using AdvantageControl.MultiTenancy;
using AdvantageControl.Net.MimeTypes;
using AdvantageControl.Roles.Dto;
using AdvantageControl.Users.Dto;
using static BaseModule.Enums.UserEnums;
using static BaseModule.Enums.FileEnums;
using BaseModule.Extensions;
using BaseModule.Helpers;
using BaseModule.Managers.BackgroundJobs.Requestable;
using BaseModule.Managers.Customers.Requestable;
using BaseModule.Managers.FileManager;
using BaseModule.Managers.Unit.Transferable;
using BaseModule.Shared;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MTR.EventBus.Shared;
using MTR.EventBus.Shared.Contracts;

namespace AdvantageControl.Users
{
    [AbpAuthorize(PermissionNames.Pages_Users)]
    public class UserAppService : AsyncCrudAppService<User, UserDto, long, PagedUserResultRequestDto, CreateUserDto, UserDto>, IUserAppService
    {
        private readonly UserManager _userManager;
        private readonly RoleManager _roleManager;
        private readonly IRepository<Role> _roleRepository;
        private readonly IFileManager _fileManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAbpSession _abpSession;
        private readonly LogInManager _logInManager;
        private readonly IRepository<UserRole, long> _userRoleRepository;
        private readonly IRepository<User, long> _userRepository;
        private readonly IRepository<CustomerEntity, long> _customerRepository;
        private readonly IRepository<UnitAssignedUserEntity> _unitAssignedUsersRepository;
        private readonly IRepository<CustomerAssignedUserEntity, int> _userAssignAccountRepository;
        private readonly TenantManager _tenantManager;
        private readonly IRepository<Tenant, int> _tenantRepository;

        public UserAppService(
            IRepository<User, long> repository,
            UserManager userManager,
            RoleManager roleManager,
            IRepository<Role> roleRepository,
            IPasswordHasher<User> passwordHasher,
            IAbpSession abpSession,
            LogInManager logInManager,
            IRepository<UserRole, long> userRoleRepository,
            IRepository<User, long> userRepository,
            IRepository<CustomerEntity, long> customerRepository,
            IRepository<CustomerAssignedUserEntity, int> userAssignAccountRepository, 
            IFileManager fileManager,
            IHttpContextAccessor httpContextAccessor,
            IRepository<UnitAssignedUserEntity> unitAssignedUsersRepository,
            TenantManager tenantManager, IRepository<Tenant, int> tenantRepository)
            : base(repository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _abpSession = abpSession;
            _logInManager = logInManager;
            _userRoleRepository = userRoleRepository;
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _userAssignAccountRepository = userAssignAccountRepository;
            _fileManager = fileManager;
            _httpContextAccessor = httpContextAccessor;
            _unitAssignedUsersRepository = unitAssignedUsersRepository;
            this._tenantManager = tenantManager;
            _tenantRepository = tenantRepository;
        }

        //Due to change in DTO not using this method
        public override async Task<UserDto> CreateAsync(CreateUserDto input)
        {
            CheckCreatePermission();

            var user = ObjectMapper.Map<User>(input);

            user.TenantId = AbpSession.TenantId;
            user.IsEmailConfirmed = true;

            await _userManager.InitializeOptionsAsync(AbpSession.TenantId);

            CheckErrors(await _userManager.CreateAsync(user, input.Password));

            if (input.RoleNames != null)
            {
                CheckErrors(await _userManager.SetRolesAsync(user, input.RoleNames));
            }

            CurrentUnitOfWork.SaveChanges();

            return MapToEntityDto(user);
        }
        //Create User
        public async Task<bool> CreateUser(UserCreator input)
        {
            try
            {
                if (await FindByEmail(input.EmailAddress) != null)
                    throw new UserFriendlyException("Email already exist.");
                var role = _roleManager.Roles.Single(r => r.Id==input.RoleId);
                var user = User.CreateTenantAdminUser(AbpSession.GetTenantId(), input.EmailAddress, input.Name, input.Surname);
                user.IsPasswordResetAllowed = input.IsPasswordResetAllowed;
                user.PasswordResetDuration = input.PasswordResetDuration;
                user.IsEmailConfirmed = false;
                user.UserStatus = UserStatusEnum.InComplete;
                user.EmailTime=DateTime.Now;
                await _userManager.InitializeOptionsAsync(_abpSession.TenantId);
                await _userManager.CreateAsync(user, User.DefaultPassword);
                await UnitOfWorkManager.Current.SaveChangesAsync();
                await _userManager.AddToRoleAsync(user, role.Name);
                
                await UnitOfWorkManager.Current.SaveChangesAsync();
                user.SetNewEmailConfirmationCode();
                var baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value +"/";

                using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
                {
                    using (var eventDispatcher = IocManager.Instance.ResolveAsDisposable<EventBusDispatcher>())
                    {
                        eventDispatcher.Object.Send<IUserEmailVerificationContract>(new UserEmailVerificationContract
                        {
                            UserId = (int)user.Id,
                            EmailConfirmationCode = user.EmailConfirmationCode,
                            FullName = user.UserName,
                            TenantId = user.TenantId ?? 0,
                            ToEmail = user.EmailAddress,
                            BaseUrl = baseUrl
                        });

                    }
                    logger.Object.InfoFormat("User verification Email Request Sent");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(ex.Message);
            }
        }

        [AllowAnonymous]
        [AbpAllowAnonymous]
        [HttpPost]
        public async Task<bool> PasswordReset(PasswordResetRequest request)
        {
            try
            {
                if (await FindByEmail(request.Email) == null)
                    throw new UserFriendlyException("Email doesn't exist.");
                var userEntity = await _userManager.FindByEmailAsync(request.Email);
                userEntity.EmailTime=DateTime.Now;
                await _userManager.UpdateAsync(userEntity);
                await CurrentUnitOfWork.SaveChangesAsync();
                userEntity.SetNewPasswordResetCode();
                var baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value + "/";

                using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
                {
                    using (var eventDispatcher = IocManager.Instance.ResolveAsDisposable<EventBusDispatcher>())
                    {
                        eventDispatcher.Object.Send<IUserEmailVerificationContract>(new UserEmailVerificationContract
                        {
                            UserId = (int)userEntity.Id,
                            EmailConfirmationCode = userEntity.PasswordResetCode,
                            FullName = userEntity.UserName,
                            TenantId = userEntity.TenantId ?? 0,
                            ToEmail = userEntity.EmailAddress,
                            BaseUrl = baseUrl
                        });

                    }
                    logger.Object.InfoFormat("Password Reset Email Request Sent");
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
        }

        [AllowAnonymous]
        [AbpAllowAnonymous]
        [HttpPut]
        public async Task<bool> UpdatePassword(UpdatePasswordRequest request)
        {
            try
            {
                if (!IsExist(request.UserId))
                    throw new UserFriendlyException("User doesn't exist.");
                var userEntity = await _userManager.GetUserByIdAsync(request.UserId);
                if (userEntity.PasswordResetCode!=null)
                {
                    throw new UserFriendlyException("Verify Your Email First to Update Password");
                }

                userEntity.Password = _userManager.PasswordHasher.HashPassword(userEntity, request.Password);
                await _userManager.UpdateAsync(userEntity);
                await CurrentUnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
        }

        [AllowAnonymous]
        [AbpAllowAnonymous]
        [HttpPost]
        public async Task<bool> ResendEmail(PasswordResetRequest request)
        {
            try
            {
                if (await FindByEmail(request.Email) == null)
                    throw new UserFriendlyException("Email doesn't exist.");

                var userEntity = await _userManager.FindByEmailAsync(request.Email);
                if (userEntity.IsEmailConfirmed)
                {
                    throw new UserFriendlyException("Email is already Confirmed.");
                }
                userEntity.EmailTime = DateTime.Now;
                await _userManager.UpdateAsync(userEntity);
                await CurrentUnitOfWork.SaveChangesAsync();
                userEntity.SetNewPasswordResetCode();
                var baseUrl = _httpContextAccessor.HttpContext.Request.Scheme + "://" + _httpContextAccessor.HttpContext.Request.Host.Value + "/";
                using (var logger = IocManager.Instance.ResolveAsDisposable<ILogger>())
                {
                    using (var eventDispatcher = IocManager.Instance.ResolveAsDisposable<EventBusDispatcher>())
                    {
                        eventDispatcher.Object.Send<IUserEmailVerificationContract>(new UserEmailVerificationContract
                        {
                            UserId = (int)userEntity.Id,
                            EmailConfirmationCode = userEntity.PasswordResetCode,
                            FullName = userEntity.UserName,
                            TenantId = userEntity.TenantId ?? 0,
                            ToEmail = userEntity.EmailAddress,
                            BaseUrl = baseUrl
                        });

                    }
                    logger.Object.InfoFormat("Password Reset Email Request Sent Again");
                }
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
        }

        [AllowAnonymous]
        [AbpAllowAnonymous]
        [HttpPost]
        [Consumes(MimeTypeNames.ApplicationJson)]
        public async Task<EmailConfirmationResponse> ConfirmEmail([FromQuery]EmailConfirmationModel request)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(p=> 
                p.EmailConfirmationCode==request.ConfirmationCode || p.PasswordResetCode==request.ConfirmationCode);
                
                if (user != null)
                {
                    if (user.EmailTime < DateTime.Now.AddHours(-24))
                    {
                        throw new UserFriendlyException("Link is Expired");
                    }
                    user.IsEmailConfirmed = true;
                    user.PasswordResetCode = null;
                    user.IsActive = true;
                    user.EmailConfirmationCode = null;

                    await _userManager.UpdateAsync(user);
                    return new EmailConfirmationResponse{UserId = user.Id};
                }

                throw new UserFriendlyException("Invalid Email Confirmation Code");
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
            
        }
       
        public async Task<bool> ModifyUser(UserDto input)
        {
            try
            {
                var user = await _userManager.GetUserByIdAsync(input.Id);
                input.Password = !input.Password.IsNullOrWhiteSpace() ? _userManager.PasswordHasher.HashPassword(user, input.Password) : user.Password;
                CopyHelper.Copy(typeof(UserDto), input, typeof(User), user);
                await _userManager.UpdateAsync(user);
                var userRole = await _userRoleRepository.FirstOrDefaultAsync(m => m.UserId == input.Id);
                if (input.RoleId != null) userRole.RoleId = (int)input.RoleId;

                var role = await _roleManager.GetRoleByNameAsync("Client");
                if (role.Id != input.RoleId)
                {
                    #region Insert/Update/Delete from UserAssignAccountList

                    var assignedAccountIdsList = await _userAssignAccountRepository.GetAll()
                        .Where(m => m.UserId == input.Id)
                        .Select(m => m.Id).ToListAsync();

                    foreach (var item in assignedAccountIdsList)
                    {
                        var check = input.UserAssignAccountList.Any(m => m.Id == item);
                        if (!check)
                        {
                            await _userAssignAccountRepository.DeleteAsync(m => m.Id == item);
                        }
                    }

                    //Delete those records which exist in DB but not exist on front end

                    foreach (var item in input.UserAssignAccountList)
                    {
                        if (item.Id == 0) //Insert
                        {
                            await _userAssignAccountRepository.InsertAsync(new CustomerAssignedUserEntity
                            {
                                CustomerId = item.CustomerId,
                                UserId = input.Id,
                                IsPrimary = item.IsPrimary,
                                IsSecondary = item.IsSecondary,
                                RoleId = item.RoleId
                            });

                        }
                        else if (item.Id != 0) //Update
                        {
                            var output = await _userAssignAccountRepository.FirstOrDefaultAsync(m => m.Id == item.Id);
                            output.IsPrimary = item.IsPrimary;
                            output.IsSecondary = item.IsSecondary;
                        }
                    }

                    #endregion
                }

                await CurrentUnitOfWork.SaveChangesAsync();
                var currentTenantId = _abpSession.GetTenantId();
                var employeeRole = await _roleManager.GetRoleByNameAsync("Employee");
                if (!input.IsActive && input.RoleId == employeeRole.Id)
                {
                    var tenant = await _tenantManager.FindByTenancyNameAsync("Default");
                    using (_abpSession.Use(tenant.Id, null))
                    {
                        using (this.UnitOfWorkManager.Current.SetTenantId(tenant.Id))
                        {
                            var currentTenant = await _tenantRepository.
                                FirstOrDefaultAsync(m => m.Id == currentTenantId);
                            if (currentTenant.ActiveSeats != 0)
                            {
                                currentTenant.ActiveSeats = currentTenant.ActiveSeats - 1;
                                await CurrentUnitOfWork.SaveChangesAsync();
                            }
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(ex.Message);
            }
        }

        [HttpPut]
        [Consumes(MimeTypeNames.ApplicationJson)]
        public async Task<bool> CompleteProfile(CompleteProfileDto input)
        {
            try
            {
                var user = await _userManager.GetUserByIdAsync(input.Id);
                if (user.EmailConfirmationCode != null)
                {
                    throw new UserFriendlyException("Verify Your Email First!");
                }
                input.Password = !input.Password.IsNullOrWhiteSpace() ? _userManager.PasswordHasher.HashPassword(user, input.Password) : user.Password;
                CopyHelper.Copy(typeof(CompleteProfileDto), input, typeof(User), user);
                
                user.UserStatus = UserStatusEnum.Complete;
                await _userManager.UpdateAsync(user);
                await CurrentUnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(ex.Message);
            }
        }

        //Due to change in return type not using this method
        public override async Task<UserDto> UpdateAsync(UserDto input)
        {
            var user = await _userManager.GetUserByIdAsync(input.Id);

            MapToEntity(input, user);
            user.Password = new PasswordHasher<User>(new OptionsWrapper<PasswordHasherOptions>
                (new PasswordHasherOptions())).HashPassword(user, input.Password);

            CheckErrors(await _userManager.UpdateAsync(user));

            var userRole = await _userRoleRepository.FirstOrDefaultAsync(m => m.UserId == input.Id);
            if (input.RoleId != null) userRole.RoleId = (int) input.RoleId;
            await CurrentUnitOfWork.SaveChangesAsync();

            return await GetAsync(input);
        }
        //Delete User
        public override async Task DeleteAsync(EntityDto<long> input)
        {
            var user = await _userManager.GetUserByIdAsync(input.Id);
            await _userManager.DeleteAsync(user);
        }
        //Get User groups other than admin
        public async Task<ListResultDto<RoleDto>> GetRoles()
        {
            var roles = await _roleRepository.GetAll().Where(m => m.Name != StaticRoleNames.Tenants.Admin && m.Name != StaticRoleNames.Tenants.SuperAdmin).OrderBy(m => m.Name).ToListAsync();
            return new ListResultDto<RoleDto>(ObjectMapper.Map<List<RoleDto>>(roles));
        }
        //Not using 
        public async Task ChangeLanguage(ChangeUserLanguageDto input)
        {
            await SettingManager.ChangeSettingForUserAsync(
                AbpSession.ToUserIdentifier(),
                LocalizationSettingNames.DefaultLanguage,
                input.LanguageName
            );
        }

        //Get users against role & search keyword
        [HttpGet]
        [Produces(MimeTypeNames.ApplicationJson)]
        public async Task<PaginatedResult<UserDto>> GetUsers(Searchable search, int roleId = 0)
        {
            try
            {
                var users = _userRepository.GetAll()
                    .Include(p => p.Roles)
                    .WhereIf(roleId > 0, p => p.Roles.Any(x => x.RoleId == roleId))
                    .WhereIf(!string.IsNullOrEmpty(search.Keyword),
                        e => e.EmailAddress.ToLower().Contains(search.Keyword.ToLower()) ||
                             e.Name.ToLower().Contains(search.Keyword.ToLower()))
                    .OrderBy(m => m.EmailAddress)
                    .GetPaged(search.Page, search.PageSize)
                    .toTransferable<UserDto>();

                if (roleId <= 0) return users;
                var role = await _roleManager.GetRoleByIdAsync(roleId);
                if (role == null) return users;
                foreach (var user in users.Items)
                {
                    user.RoleId = role.Id;
                    user.RoleNames = new[] {role.Name};
                }

                return users;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
        }

        //Get users against Customer & search keyword
        [HttpGet]
        [Produces(MimeTypeNames.ApplicationJson)]
        public async Task<List<AssignedUsers>> GetCustomerUsers([FromQuery]Searchable search)
        {
            try
            {
                var unitAssignedUserList = new List<long>();
                if (search.UnitId > 0)
                {
                    unitAssignedUserList = await _unitAssignedUsersRepository.GetAll()
                        .Where(p => p.UnitId == search.UnitId)
                        .Select(p => p.UserId).ToListAsync();
                }
                var assignedUsers = new List<AssignedUsers>();
                var paginatedEntityResult = await _userAssignAccountRepository.GetAll()
                    .Include(p => p.User)
                    .WhereIf(search.ItemId > 0, p => p.CustomerId == search.ItemId)
                    .WhereIf(search.UnitId > 0, p => !unitAssignedUserList.Contains(p.UserId))
                    .Where(p => p.RoleId != 3 && p.User.Name != "Super Admin")
                    .OrderBy(p=> p.User.Name)
                    .Select(p=> new AssignedUsers
                    {
                        Id = p.UserId,
                        UserName = p.User.Name,
                        IsPrimary = p.IsPrimary,
                        Email = p.User.EmailAddress
                    })
                    .ToListAsync();
                if (paginatedEntityResult != null)
                {
                    var lists = paginatedEntityResult.Select(p => p.Id).Distinct().ToList();

                    foreach (var list in lists)
                    {
                        assignedUsers.Add(paginatedEntityResult.FirstOrDefault(p => p.Id == list));
                    }
                }

                return assignedUsers;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
            
        }

        [HttpGet]
        [Produces(MimeTypeNames.ApplicationJson)]
        public async Task<List<AssignedUsers>> GetUnitUsers([FromQuery]Searchable search)
        {
            try
            {
                var unitAssignedUserList = await _unitAssignedUsersRepository.GetAll()
                     .Include(p => p.User)
                     .Where(p => p.UnitId == search.UnitId)
                     .Where(m=>m.User.Name != "Super Admin")
                     .OrderBy(p => p.User.Name)
                     .Select(p => new AssignedUsers
                     {
                         Id = p.UserId,
                         UserName = p.User.Name,
                         IsPrimary = p.IsPrimary
                     })
                     .ToListAsync();

                var assignedUsers = new List<AssignedUsers>();

                if (unitAssignedUserList == null) return assignedUsers;
                {
                    var lists = unitAssignedUserList.Select(p => p.Id).Distinct().ToList();

                    foreach (var list in lists)
                    {
                        assignedUsers.Add(unitAssignedUserList.FirstOrDefault(p => p.Id == list));
                    }
                }

                return assignedUsers;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
        }


        //Get users against Customer & search keyword
        [HttpGet]
        [Produces(MimeTypeNames.ApplicationJson)]
        public async Task<List<AssignedUsers>> GetAllCustomerUsers([FromQuery]Searchable search)
        {
            try
            {
                var unitAssignedUserList = new List<long>();
                if (search.UnitId > 0)
                {
                    unitAssignedUserList = await _unitAssignedUsersRepository.GetAll()
                        .Where(p => p.UnitId == search.UnitId)
                        .Select(p => p.UserId).ToListAsync();
                }
                var assignedUsers = new List<AssignedUsers>();
                var paginatedEntityResult = await _userAssignAccountRepository.GetAll()
                    .Include(p => p.User)
                    .WhereIf(search.ItemId > 0, p => p.CustomerId == search.ItemId)
                    .WhereIf(search.UnitId > 0, p => unitAssignedUserList.Contains(p.UserId))
                    .Where(m=>m.User.Name != "Super Admin")
                    .OrderBy(p => p.User.Name)
                    .Select(p => new AssignedUsers
                    {
                        Id = p.UserId,
                        UserName = p.User.Name
                    })
                    .ToListAsync();
                if (paginatedEntityResult != null)
                {
                    var lists = paginatedEntityResult.Select(p => p.Id).Distinct().ToList();

                    foreach (var list in lists)
                    {
                        assignedUsers.Add(paginatedEntityResult.FirstOrDefault(p => p.Id == list));
                    }
                }

                return assignedUsers;
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw new UserFriendlyException(e.Message);
            }
           
        }

        //Not using
        protected override User MapToEntity(CreateUserDto createInput)
        {
            var user = ObjectMapper.Map<User>(createInput);
            user.SetNormalizedNames();
            return user;
        }
        //Mapping
        protected override void MapToEntity(UserDto input, User user)
        {
            ObjectMapper.Map(input, user);
            user.SetNormalizedNames();
        }

        //Not using
        protected override UserDto MapToEntityDto(User user)
        {
            var roleIds = user.Roles.Select(x => x.RoleId).ToArray();

            var roles = _roleManager.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.NormalizedName);

            var userDto = base.MapToEntityDto(user);
            userDto.RoleNames = roles.ToArray();

            return userDto;
        }

        //Not using
        protected override IQueryable<User> CreateFilteredQuery(PagedUserResultRequestDto input)
        {
            return Repository.GetAllIncluding(x => x.Roles)
                .WhereIf(!input.Keyword.IsNullOrWhiteSpace(), x => x.UserName.Contains(input.Keyword) || x.Name.Contains(input.Keyword) || x.EmailAddress.Contains(input.Keyword))
                .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
        }

        //Not using
        protected override async Task<User> GetEntityByIdAsync(long id)
        {
            var user = await Repository.GetAllIncluding(x => x.Roles).FirstOrDefaultAsync(x => x.Id == id);
            
            if (user == null)
            {
                throw new EntityNotFoundException(typeof(User), id);
            }

            return user;
        }
        //Find by id
        public async Task<UserDto> FindById(long userId)
        {
            try
            {
                var user = await _userRepository.GetAllIncluding(p => p.Roles).FirstOrDefaultAsync(m => m.Id == userId);
                var userDto = ObjectMapper.Map<UserDto>(user);
                userDto.Password = user.Password;
                userDto.RoleId = user.Roles.FirstOrDefault()?.RoleId;
                var role = await _roleManager.FindByNameAsync("Client");
                userDto.UserAssignAccountList = new List<UserAssignAccountListCreator>();
                userDto.UserAssignAccountList = await _userAssignAccountRepository.GetAll()
                    .Where(m => m.UserId == userId && m.RoleId != role.Id).Select(m =>
                        new UserAssignAccountListCreator
                        {
                            Id = m.Id,
                            CustomerId = m.CustomerId,
                            UserId = m.UserId,
                            IsPrimary = m.IsPrimary,
                            IsSecondary = m.IsSecondary,
                            RoleId = m.RoleId
                        }).ToListAsync();
                foreach (var item in userDto.UserAssignAccountList)
                {
                    var customer = await _customerRepository.FirstOrDefaultAsync(item.CustomerId);
                    if (customer != null)
                    {
                        item.CustomerName = customer.Name;
                    }
                }

                userDto.PictureUrl = (await _fileManager.GetFiles(userId, FeatureEnum.Profile)).FirstOrDefault();
                return userDto;
            }
            catch (Exception ex)
            {
                throw new UserFriendlyException(ex.Message);
            }
        }

        private bool IsExist(long id)
        {
            var user=  _userRepository.FirstOrDefaultAsync(p => p.Id == id);
            return user != null;
        }

        public async Task<List<CustomersListDto>> GetCustomersList(int roleId)
        {
            var customersList = new List<CustomersListDto>();
            var role = await _roleManager.GetRoleByNameAsync("Client");
            if (role.Id == roleId) return customersList;
            customersList = await _customerRepository.GetAll().Select(m => new CustomersListDto
            {
                CustomerId = m.Id,
                CustomerName = m.Name,
                RoleId = roleId,
                IsPrimary = false,
                IsSecondary = false
            }).ToListAsync();

            return customersList;

        }

        //Not using
        protected override IQueryable<User> ApplySorting(IQueryable<User> query, PagedUserResultRequestDto input)
        {
            return query.OrderBy(r => r.UserName);
        }
        //Not using
        protected virtual void CheckErrors(IdentityResult identityResult)
        {
            identityResult.CheckErrors(LocalizationManager);
        }
        //Not using
        public async Task<bool> ChangePassword(ChangePasswordDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attemping to change password.");
            }
            long userId = _abpSession.UserId.Value;
            var user = await _userManager.GetUserByIdAsync(userId);
            var loginAsync = await _logInManager.LoginAsync(user.UserName, input.CurrentPassword, shouldLockout: false);
            if (loginAsync.Result != AbpLoginResultType.Success)
            {
                throw new UserFriendlyException("Your 'Existing Password' did not match the one on record.  Please try again or contact an administrator for assistance in resetting your password.");
            }
            if (!new Regex(AccountAppService.PasswordRegex).IsMatch(input.NewPassword))
            {
                throw new UserFriendlyException("Passwords must be at least 8 characters, contain a lowercase, uppercase, and number.");
            }
            user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
            CurrentUnitOfWork.SaveChanges();
            return true;
        }
        //Not using
        public async Task<bool> ResetPassword(ResetPasswordDto input)
        {
            if (_abpSession.UserId == null)
            {
                throw new UserFriendlyException("Please log in before attemping to reset password.");
            }
            long currentUserId = _abpSession.UserId.Value;
            var currentUser = await _userManager.GetUserByIdAsync(currentUserId);
            var loginAsync = await _logInManager.LoginAsync(currentUser.UserName, input.AdminPassword, shouldLockout: false);
            if (loginAsync.Result != AbpLoginResultType.Success)
            {
                throw new UserFriendlyException("Your 'Admin Password' did not match the one on record.  Please try again.");
            }
            if (currentUser.IsDeleted || !currentUser.IsActive)
            {
                return false;
            }
            var roles = await _userManager.GetRolesAsync(currentUser);
            if (!roles.Contains(StaticRoleNames.Tenants.Admin))
            {
                throw new UserFriendlyException("Only administrators may reset passwords.");
            }

            var user = await _userManager.GetUserByIdAsync(input.UserId);
            if (user != null)
            {
                user.Password = _passwordHasher.HashPassword(user, input.NewPassword);
                CurrentUnitOfWork.SaveChanges();
            }

            return true;
        }
        private async Task<User> FindByEmail(string email)
        {
            return await _userRepository.FirstOrDefaultAsync(m => m.EmailAddress == email);
        }
    }
}

