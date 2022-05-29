namespace Common
{
    public static class Variables
    {
        public static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "renderphine");
        public static readonly string Version = Init.GetVersion();

        static Variables() => Directory.CreateDirectory(ConfigDirectory);
    }
}