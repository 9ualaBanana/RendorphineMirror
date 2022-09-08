using Node.Trasnport.Models;
using System.Security.Cryptography;
using System.Text;

namespace Node.Trasnport.Download;

// ? Define API for checking if the file is already downloaded or is partially downloaded.
internal class DownloadFilesManager
{
    internal readonly string FileId;
    internal string FullName;
    readonly FileInfo _partFile;
    readonly FileInfo _pcksFile;
    internal readonly List<LongRange> DownloadedBytes;

    internal DownloadFilesManager(DownloadFileInfo fileInfo)
    {
        FileId = string.Join(null, SHA512.HashData(
            Encoding.UTF8.GetBytes($"{fileInfo.SessionId}{fileInfo.Name}{fileInfo.Extension}{fileInfo.Size}"))[..16].Select(b => b.ToString()
            ));
        FullName = FileId + fileInfo.Extension;
        _partFile = new FileInfo(FullName + ".part");
        _pcksFile = new FileInfo(FileId + ".pcks");
        // Should check for files whose name start with FileId because I need to know if that file is already downloaded
        // and not only if it didn't finish downloading.
        if (!_partFile.Exists)
        {
            using var fs = _partFile.Create();
            fs.SetLength(fileInfo.Size);

            using var _ = _pcksFile.Create();
        }
        DownloadedBytes = ReadDownloadedBytes();
    }

    List<LongRange> ReadDownloadedBytes()
    {
        var byteRanges = new List<LongRange>();
        using var sr = _pcksFile.OpenText();
        string? line;
        while ((line = sr.ReadLine()) is not null)
        {
            var unparsedRange = line.Split();
            long start = long.Parse(unparsedRange.First());
            long end = long.Parse(unparsedRange.Last());
            byteRanges.Add(new(start, end));
        }
        return byteRanges;
    }

    internal async Task DownloadPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        if (IsDownloaded(packet)) throw new Exception("Some bytes from that packet are already uploaded.");

        await WriteToPartFileAsync(packet, cancellationToken);
        UpdateDownloadedBytes(packet);
    }

    async Task WriteToPartFileAsync(Packet packet, CancellationToken cancellationToken)
    {
        using var fs = _partFile.OpenWrite();
        fs.Position = packet.Offset;
        await packet.Content.CopyToAsync(fs, cancellationToken);
    }

    void UpdateDownloadedBytes(Packet packet)
    {
        var start = packet.Offset;
        var end = packet.Offset + packet.Content.Length;

        File.AppendAllLines(_pcksFile.FullName, new string[] { $"{start} {end}" });
        DownloadedBytes.Add(new(start, end));
    }

    internal void FinalizeDownload()
    {
        // MoveTo throws if the file with the same name already exists (i.e. if that file was previously downloaded).
        _partFile.MoveTo(Path.ChangeExtension(_partFile.FullName, null));
        _pcksFile.Delete();
    }

    bool IsDownloaded(Packet packet)
    {
        return DownloadedBytes.Any(range => range.IsInRange(packet.Offset));
    }
}
