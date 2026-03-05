public static class NetUtils
{
    public static bool IsNewerTick(ushort a, ushort b)
    {
        return (short)(a - b) > 0;
    }
}