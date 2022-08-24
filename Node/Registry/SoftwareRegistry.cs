namespace Node.Registry;

public static class SoftwareRegistry
{
    public static void Install(SoftwareVersionDefinition soft) => PowerShellInvoker.Invoke(soft.InstallScript);
}
