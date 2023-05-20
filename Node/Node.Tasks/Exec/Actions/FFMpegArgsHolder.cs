using System.Collections;

namespace Node.Tasks.Exec.Actions;

public class FFMpegArgsHolder
{
    public readonly FFProbe.FFProbeInfo FFProbe;
    public double Rate = 1;

    public FileFormat OutputFileFormat;
    public string? OutputFileName;
    public readonly ArgList Args = new();
    public readonly OrderList<string> AudioFilers = new();
    public readonly OrderList<string> Filtergraph = new();

    public FFMpegArgsHolder(FileFormat outputFileFormat, FFProbe.FFProbeInfo ffprobe)
    {
        OutputFileFormat = outputFileFormat;
        FFProbe = ffprobe;
    }



    public class OrderList<T> : IEnumerable<T>
    {
        public int Count => Items.Count + ItemsLast.Count;

        readonly List<T> Items = new();
        readonly List<T> ItemsLast = new();

        public void AddFirst(T item) => Items.Insert(0, item);
        public void Add(T item) => Items.Add(item);
        public void AddLast(T item) => ItemsLast.Add(item);

        public IEnumerator<T> GetEnumerator() => Items.Concat(ItemsLast).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}