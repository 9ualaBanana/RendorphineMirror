using System.IO.Compression;
using System.Management.Automation;

namespace Node.Registry;

[Cmdlet("Expand", "Plugin-Archive")]
public class EXpandPluginArchive : PSCmdlet
{
    protected override void ProcessRecord()
    {
        base.ProcessRecord();


        var pldownload = this.GetVariableValue("PLDOWNLOAD").ToString()!;
        var plinstall = this.GetVariableValue("PLINSTALL").ToString()!;
        Directory.CreateDirectory(plinstall);

        var zip = Directory.GetFiles(pldownload, "*.zip").Single();
        ZipFile.ExtractToDirectory(Path.Combine(pldownload, zip), plinstall);
    }
}
