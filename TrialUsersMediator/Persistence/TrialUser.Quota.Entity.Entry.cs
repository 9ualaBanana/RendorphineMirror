using System.Collections;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        public partial record Entity
        {
            public record Entry(EQuota Type, int Value)
            {
                internal Entity Quota { get; private set; } = default!;


                public class Collection : ICollection<Entry>
                {
                    internal Entity Quota { get; private set; } = default!;

                    readonly Dictionary<EQuota, int> _quotaEntries = new();

                    internal static Builder From(Dictionary<EQuota, int> quotaEntries)
                        => new(quotaEntries);

                    Collection(Dictionary<EQuota, int> quotaEntries)
                    { _quotaEntries = quotaEntries; }
                    public Collection() { }


                    internal class Builder
                    {
                        readonly Collection _quotaEntries;

                        internal Builder(Dictionary<EQuota, int> quotaEntries)
                        { _quotaEntries = new Collection(quotaEntries); }

                        internal Collection OwnedBy(Entity quotaEntity)
                        { _quotaEntries.Quota = quotaEntity; return _quotaEntries; }
                    }

                    #region ICollection

                    public int Count => _quotaEntries.Count;

                    bool ICollection<Entry>.IsReadOnly => true;

                    void ICollection<Entry>.Add(Entry quotaEntry)
                        => _quotaEntries.Add(quotaEntry.Type, quotaEntry.Value);

                    void ICollection<Entry>.Clear()
                        => _quotaEntries.Clear();

                    bool ICollection<Entry>.Contains(Entry quotaEntry)
                        => _quotaEntries.ContainsKey(quotaEntry.Type) && _quotaEntries.ContainsValue(quotaEntry.Value);

                    void ICollection<Entry>.CopyTo(Entry[] array, int arrayIndex)
                    {
                        throw new NotImplementedException();
                    }

                    bool ICollection<Entry>.Remove(Entry quotaEntry)
                        => _quotaEntries.Remove(quotaEntry.Type);

                    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                    public IEnumerator<Entry> GetEnumerator()
                        => _quotaEntries.Select(_ => new Entry(_.Key, _.Value) { Quota = Quota }).GetEnumerator();

                    #endregion
                }
            }
        }
    }
}
