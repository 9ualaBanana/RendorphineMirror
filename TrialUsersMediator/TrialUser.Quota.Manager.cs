using Node.Tasks.Models;

namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        internal class Manager
        {
            // Thrad-safety kludge that doesn't distinguish between trial users and locks for any quota management action.
            readonly static object _managerLock = new { };

            internal int Value
            {
                get
                {
                    lock (_managerLock)
                    { return _trialUser.Quota_._entries[_taskAction]; }
                }
            }

            readonly TrialUser.Entity _trialUser;
            readonly TaskAction _taskAction;

            internal static Manager For(TrialUser.Entity trialUser, TaskAction taskAction)
                => new(trialUser, taskAction);

            Manager(TrialUser.Entity trialUser, TaskAction taskAction)
            {
                _trialUser = trialUser;
                _taskAction = taskAction;
            }

            internal void Decrease() => DecreaseBy(1);

            internal void DecreaseBy(int count)
            {
                lock (_managerLock)
                { _trialUser.Quota_._entries[_taskAction] -= count; }
            }

            internal void Increase() => IncreaseBy(1);

            internal void IncreaseBy(int count)
            {
                lock (_managerLock)
                { _trialUser.Quota_._entries[_taskAction] += count; }
            }
        }
    }
}