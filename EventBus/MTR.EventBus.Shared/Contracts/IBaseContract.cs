namespace MTR.EventBus.Shared.Contracts
{
    public interface IContractable {

    }

    public interface IBaseContract : IContractable
    {
        int TenantId { get; }
        int UserId { get; }
    }
}
