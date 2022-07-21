namespace Node.P2P.Models;

internal record LongRange(long Start, long End)
{
    internal long Length => End - Start;

    internal bool IsInRange(long value) => Start <= value && value < End;
}
