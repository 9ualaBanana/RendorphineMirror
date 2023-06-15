using NodeCommon.Tasks;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        internal class Manager
        {
            internal int Value
            { get { lock (_quotaLock) { return _managed._entries[_quota]; } } }
                
            readonly Quota<EQuota> _managed;
            readonly EQuota _quota;
            readonly object _quotaLock = new { };

            internal static Manager For(Quota<EQuota> quota, EQuota concreteQuota)
                => new(quota, concreteQuota);

            Manager(Quota<EQuota> managed, EQuota quota)
            {
                _managed = managed;
                _quota = quota;
            }

            internal void Decrease() => DecreaseBy(1);

            internal void DecreaseBy(int count)
            { lock (_quotaLock) { _managed._entries[_quota] -= count; } }

            internal void Increase() => IncreaseBy(1);

            internal void IncreaseBy(int count)
            { lock (_quotaLock) { _managed._entries[_quota] += count; } }
        }
    }
}
