namespace MTR.EventBus.Shared.Contracts
{
    public interface IUserEmailVerificationContract: IBaseContract
    { 
        string BaseUrl { get; set; }
        string ToEmail { get; set; }
        string EmailConfirmationCode { get; set; }
        string FullName { get; set; }
    }

    public class UserEmailVerificationContract : IUserEmailVerificationContract
    {
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string BaseUrl { get; set; }
        public string ToEmail { get; set; }
        public string EmailConfirmationCode { get; set; }
        public string FullName { get; set; }
    }
}
