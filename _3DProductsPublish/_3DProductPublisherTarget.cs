using _3DProductsPublish.CGTrader.Api;
using _3DProductsPublish.CGTrader.Upload;
using _3DProductsPublish.Turbosquid;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using CefSharp.OffScreen;

namespace _3DProductsPublish;

public class _3DProductPublisherTarget : IDelayedServiceTarget
{
    public static IEnumerable<IComponentRegistration> CreateRegistrations() => [];

    public required CGTraderTarget CGTrader { get; init; }
    public required TurboSquidTarget TurboSquid { get; init; }

    public void Activated()
    {
        if (OperatingSystem.IsWindows())
            CefSharp.Cef.Initialize(new CefSettings { PersistSessionCookies = true, CachePath = Path.GetFullPath("cef_cache") });
    }


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
            yield return RegistrationBuilder.ForType<TurboSquidContainer>()
                .AsSelf()
                .AsImplementedInterfaces()
                .SingleInstance()
                .CreateRegistration();
        }
    }
}
