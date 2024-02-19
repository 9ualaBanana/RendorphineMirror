using System.Net;
using _3DProductsPublish.CGTrader.Api;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid.Upload;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace _3DProductsPublish;

public class _3DProductPublisherTarget : IDelayedServiceTarget
{
    public static IEnumerable<IComponentRegistration> CreateRegistrations() => [];

    public required CGTraderTarget CGTrader { get; init; }
    public required TurboSquidTarget TurboSquid { get; init; }


    public class CGTraderTarget : IDelayedServiceTarget
    {
        public static IEnumerable<IComponentRegistration> CreateRegistrations()
        {
            yield return RegistrationBuilder.ForType<CGTraderApi>()
                .InstancePerDependency()
                .CreateRegistration();

            yield return RegistrationBuilder.ForType<CGTraderCaptchaApi>()
                .InstancePerDependency()
                .CreateRegistration();

            yield return RegistrationBuilder.ForType<CGTrader3DProductPublisher>()
                .AsSelf()
                .AsImplementedInterfaces()
                .InstancePerDependency()
                .CreateRegistration();
        }
    }
    public class TurboSquidTarget : IDelayedServiceTarget
    {
        public static IEnumerable<IComponentRegistration> CreateRegistrations()
        {
            yield return RegistrationBuilder.ForDelegate((ctx, _) =>
            {
                var username = ctx.Resolve<INodeSettings>().TurboSquidUsername;
                var password = ctx.Resolve<INodeSettings>().TurboSquidPassword;

                return _3DProductsPublish.Turbosquid.Upload.TurboSquid.LogInAsyncUsing(new NetworkCredential(username, password), ctx.Resolve<INodeGui>(), default)
                    .GetAwaiter().GetResult();
            })
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance()
                .CreateRegistration();
        }
    }
}
