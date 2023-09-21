namespace Node.Tests;

public static class Container
{
    public static readonly IContainer Instance = CreateBuilder().Build();

    public static ContainerBuilder CreateBuilder() => Init.CreateContainer(new Init.InitConfig("renderfin-test"));
}
