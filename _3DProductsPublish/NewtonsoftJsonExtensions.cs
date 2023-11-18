using System.Net.Mime;
using System.Text;

namespace _3DProductsPublish;

static class NewtonsoftJsonExtensions
{
    internal static StringContent ToJsonContent(this JObject jObject, Formatting formatting = Formatting.None)
        => new StringContent(jObject.ToString(formatting), Encoding.UTF8, MediaTypeNames.Application.Json);
}
