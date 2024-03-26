﻿using MarkTM.RFProduct;

namespace _3DProductsPublish._3DProductDS;

public class _3DProductThumbnail(string path, DateTime lastWriteTime) : I3DProductAsset, IEquatable<_3DProductThumbnail>
{
    public string Path { get; init; } = path;
    public DateTime LastWriteTime { get; set; } = lastWriteTime;

    public _3DProductThumbnail(string path)
        : this(path, File.GetLastWriteTimeUtc(path))
    {
    }
    protected _3DProductThumbnail(_3DProductThumbnail origin)
        : this(origin.Path, origin.LastWriteTime)
    {
    }

    internal static IEnumerable<_3DProductThumbnail> EnumerateAt(string _3DProductDirectory) =>
        Directory.EnumerateFiles(_3DProductDirectory)
        .Where(RFProduct._3D.Idea_.IsRender)
        .Select(_ => new _3DProductThumbnail(_));

    public override bool Equals(object? obj) => Equals(obj as _3DProductThumbnail);
    public bool Equals(_3DProductThumbnail? other)
        => this.Name() == other?.Name() && this.Size() == other?.Size();
    public override int GetHashCode() => HashCode.Combine(this.Name(), this.Size());
}
