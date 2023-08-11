using Node.Listeners;

namespace Node.Services;

public static class ListenerRegistrator
{
    public static void RegisterListener<T>(this ContainerBuilder builder) where T : ListenerBase =>
        builder.RegisterType<T>()
            .SingleInstance()
            .OnActivating(l => l.Instance.Start());
}
