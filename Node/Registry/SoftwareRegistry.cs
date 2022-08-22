namespace Node.Registry;

public static class SoftwareRegistry
{
    public static ValueTask<OperationResult<ImmutableArray<SoftwareDefinition>>> GetSoftware() =>
        LocalApi.Send<ImmutableArray<SoftwareDefinition>>(Settings.RegistryUrl, "getsoft");

    public static void Install(SoftwareVersionDefinition soft) => PowerShellInvoker.Invoke(soft.InstallScript);
}
