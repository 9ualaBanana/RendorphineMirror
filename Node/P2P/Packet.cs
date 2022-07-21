using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Node.P2P;

internal record Packet(
    string FileName,
    string FileId,
    long Offset,
    Stream Content)
{
    internal async Task<MultipartFormDataContent> ToHttpContentAsync()
    {
        var content = new byte[Content.Length];
        await Content.ReadAsync(content);
        return new()
        {
            { new StringContent(FileId), "fileid" },
            { new StringContent(Offset.ToString()), "offset" },
            { new ByteArrayContent(content), "chunk", FileName }
        };
    }

    internal static async Task<Packet> DeserializeAsync(HttpListenerRequest request)
    {
        var boundary = request.ContentType![(request.ContentType!.IndexOf('"') + 1)..^1];
        var multipartReader = new MultipartReader(boundary, request.InputStream);

        var fileId = await (await multipartReader.ReadNextSectionAsync()).ReadAsStringAsync();
        var offset = long.Parse(await
            (await multipartReader.ReadNextSectionAsync())
            .ReadAsStringAsync());
        var fileSection = (await multipartReader.ReadNextSectionAsync()).AsFileSection();

        return new(fileSection.FileName, fileId, offset, fileSection.FileStream);
    }
}
