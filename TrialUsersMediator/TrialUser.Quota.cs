namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        Dictionary<EQuota, int> _entries { get; set; } = default!;

        internal static Quota<EQuota> Default => new(
            Enum.GetValues<EQuota>()
            .ToLookup(quotaType => quotaType, _ => _DefaultQuota)
            .ToDictionary(quotaType => quotaType.Key, quota => quota.Single())
            );
        protected const int _DefaultQuota = 5;

        Quota() { }
        protected Quota(Quota<EQuota> quota)
        { _entries = quota._entries; }
        Quota(Dictionary<EQuota, int> entries)
        { _entries = entries; }
    }
}
