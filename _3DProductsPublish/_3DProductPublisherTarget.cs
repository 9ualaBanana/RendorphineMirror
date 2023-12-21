using _3DProductsPublish.CGTrader.Api;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid.Upload;
using Autofac;
using Node.Common.Models;

namespace _3DProductsPublish;

public class _3DProductPublisherTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required CGTraderTarget CGTrader { get; init; }
    public required TurboSquidTarget TurboSquid { get; init; }


    public class CGTraderTarget : IServiceTarget
    {
        public static void CreateRegistrations(ContainerBuilder builder)
        {
            builder.RegisterType<CGTraderApi>()
                .InstancePerDependency();

            builder.RegisterType<CGTraderCaptchaApi>()
                .InstancePerDependency();

            builder.RegisterType<CGTrader3DProductPublisher>()
                .AsSelf()
                .AsImplementedInterfaces()
                .InstancePerDependency();
        }
    }
    public class TurboSquidTarget : IServiceTarget
    {
        public static void CreateRegistrations(ContainerBuilder builder)
        {
            builder.RegisterType<TurboSquid3DProductPublisher>()
                .AsSelf()
                .AsImplementedInterfaces()
                .InstancePerDependency();
        }
    }
}
