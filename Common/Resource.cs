using System.Reflection;

namespace Common
{
    public class Resource
    {
        public static Stream LoadStream<T>(string path) => LoadStream(typeof(T), path);
        public static Stream LoadStream<T>(T obj, string path) where T : notnull => LoadStream(obj.GetType(), path);
        public static Stream LoadStream(Type type, string path) => LoadStream(type.Assembly, path);
        public static Stream LoadStream(Assembly assembly, string path) =>
            assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources." + path) ?? throw new NullReferenceException();
    }
}