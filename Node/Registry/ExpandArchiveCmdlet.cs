using System.IO.Compression;
using System.Management.Automation;

namespace Node.Registry;

[Cmdlet("Expand", "Archive")]
public class ExpandArchiveCmdlet : PSCmdlet
{
    [Parameter(Position = 0, Mandatory = true)]
    public string LiteralPath { get; set; } = null!;

    [Parameter(Position = 1, Mandatory = true)]
    public string Destinationpath { get; set; } = null!;

    protected override void ProcessRecord()
    {
        base.ProcessRecord();
        ZipFile.ExtractToDirectory(LiteralPath, Destinationpath);
    }
}
