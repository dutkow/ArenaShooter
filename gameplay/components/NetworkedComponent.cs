using Godot;

public partial class NetworkedComponent : Node
{
    public NetworkRole Role { get; private set; } = NetworkRole.NONE;

    private bool _isAuthority = false;

    public bool IsLocal => Role == NetworkRole.LOCAL;
    public bool IsRemote => Role == NetworkRole.REMOTE;

    public bool IsAuthority => _isAuthority;

    public void SetRole(NetworkRole role)
    {
        if (Role == role)
        {
            return;
        }

        Role = role;
    }

    public void SetAuthority(bool isAuthority)
    {
        if(_isAuthority == isAuthority)
        {
            return;
        }

        _isAuthority = isAuthority;
    }
}