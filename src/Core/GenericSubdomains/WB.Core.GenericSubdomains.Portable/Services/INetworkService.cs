namespace WB.Core.GenericSubdomains.Portable.Services
{
    public interface INetworkService
    {
        bool IsNetworkEnabled();
        string GetNetworkType();
        string GetNetworkName();
    }
}
