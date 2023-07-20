namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        /// <summary>
        /// This field won't contain a value when an entity, whose base class is this class,
        /// being constructed as a result of database query.
        /// </summary>
        protected Dictionary<EQuota, int> _entries;

        internal static Quota<EQuota> Default => new(
            Enum.GetValues<EQuota>()
            .ToLookup(quotaType => quotaType, _ => _DefaultQuota)
            .ToDictionary(quotaType => quotaType.Key, quota => quota.Single())
            );
        protected const int _DefaultQuota = 5;

        protected Quota() { _entries = default!; }
        Quota(Dictionary<EQuota, int> entries)
        { _entries = entries; }

        protected Quota(Quota<EQuota> quota)
        { _entries = quota._entries; }
    }
}
