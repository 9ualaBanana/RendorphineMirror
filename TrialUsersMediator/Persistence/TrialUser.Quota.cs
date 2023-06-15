using NodeCommon.Tasks;

namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        protected Dictionary<EQuota, int> _entries;

        internal static Quota<EQuota> Default => new(
            Enum.GetValues<EQuota>()
            .ToLookup(taskAction => taskAction, _ => _DefaultQuota)
            .ToDictionary(quotaType => quotaType.Key, quota => quota.Single())
            );
        protected const int _DefaultQuota = 5;

        protected Quota(Dictionary<EQuota, int> entries)
        { _entries = entries; }

        protected Quota(Quota<EQuota> quota)
        { _entries = quota._entries; }
        protected Quota() { _entries = default!; }

        internal Quota<EQuota>.Manager For(EQuota quota)
            => Quota<EQuota>.Manager.For(this, quota);
    }

    internal record TaskQuota : Quota<TaskAction>
    {
        new internal static TaskQuota Default => new(
            Enum.GetValues<TaskAction>()
            .ToLookup(taskAction => taskAction, _ => _DefaultQuota)
            .ToDictionary(taskAction => taskAction.Key, quota => quota.Single())
            );

        TaskQuota(Dictionary<TaskAction, int> quota)
            : base(quota)
        {
        }
    }
}
