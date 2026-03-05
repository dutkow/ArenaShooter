public interface INetworkedObject
{
    public bool IsAuthority();
    public NetworkRole GetNetworkRole();
}