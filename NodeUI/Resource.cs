using System.Reflection;

namespace NodeUI
{
    public class Resource
    {
        public static Stream LoadStream(string path) => LoadStream(typeof(Resource).Assembly, path);
        public static Stream LoadStream(Assembly assembly, string path) =>
            assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + path) ?? throw new NullReferenceException();
    }
}