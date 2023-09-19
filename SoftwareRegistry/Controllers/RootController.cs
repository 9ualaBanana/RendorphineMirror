namespace SoftwareRegistry.Controllers;

[ApiController]
public class RootController : ControllerBase
{
    readonly string SchemaJson;

    public RootController()
    {
        using var stream = GetType().Assembly.GetManifestResourceStream("SoftwareRegistry.Resources.schema.json").ThrowIfNull("Schema json not found");
        using var reader = new StreamReader(stream);
        using var jreader = new JsonTextReader(reader);
        SchemaJson = JToken.Load(jreader).ToString(Formatting.None);
    }

    [HttpGet("pluginschema")]
    public IResult GetPluginSchema() => Results.Text(SchemaJson, contentType: "application/json");
}
