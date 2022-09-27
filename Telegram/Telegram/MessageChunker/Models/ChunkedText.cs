namespace Telegram.Telegram.MessageChunker.Models;

public class ChunkedText : IEquatable<ChunkedText>
{
    readonly string _content;
    readonly int _chunkSize;
    int _pointer;


    public bool IsChunked => _content.Length <= _chunkSize;
    public bool IsAtMiddle => !IsAtFirstChunk && !IsAtLastChunk;
    public bool IsAtFirstChunk => _pointer == 0;
    public bool IsAtLastChunk =>
        _pointer == _content.Length || _pointer + _chunkSize >= _content.Length;


    public ChunkedText(string content, int chunkSize = 4096)
    {
        if (chunkSize == 0) throw new ArgumentOutOfRangeException(
            nameof(chunkSize), chunkSize, $"{nameof(chunkSize)} can't be 0.");

        _content = content;
        _chunkSize = chunkSize;
        _pointer = 0;
    }


    public string PreviousChunk()
    {
        int chunkStart = _pointer - _chunkSize;
        if (chunkStart < 0) chunkStart = 0;
        _pointer -= chunkStart;

        return NextChunk(movePointerToEnd: false);
    }

    public string NextChunk(bool movePointerToEnd = true)
    {
        int chunkEnd = _pointer + _chunkSize;
        if (chunkEnd > _content.Length) chunkEnd = _content.Length;
        if (movePointerToEnd) _pointer = chunkEnd;

        return _content[_pointer..chunkEnd];
    }

    #region Equality
    public override bool Equals(object? obj) => Equals(obj as ChunkedText);
    public bool Equals(ChunkedText? other) => Equals(other);
    public override int GetHashCode() => _content.GetHashCode();
    #endregion

    #region Conversions
    public static implicit operator string(ChunkedText chunkedMessage) => chunkedMessage._content;
    #endregion
}
