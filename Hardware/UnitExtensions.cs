namespace Hardware;

public static class UnitExtensions
{
    public static double KB(this long b) => b / 1024;
    public static double KB(this ulong b) => b / 1024;
    public static double KB(this double b) => b / 1024;

    public static double MB(this long kb) => kb / 1024;
    public static double MB(this ulong kb) => kb / 1024;
    public static double MB(this double kb) => kb / 1024;

    public static double GB(this long mb) => mb / 1024;
    public static double GB(this ulong mb) => mb / 1024;
    public static double GB(this double mb) => mb / 1024;

    public static double GHz(this long mHz) => mHz / 1000;
    public static double GHz(this ulong mHz) => mHz / 1000;
    public static double GHz(this double mHz) => mHz / 1000;
}
