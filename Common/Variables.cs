namespace Common
{
    public static class Variables
    {
        public static readonly string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "renderphine");

        static Variables() => Directory.CreateDirectory(ConfigDirectory);
    }
}