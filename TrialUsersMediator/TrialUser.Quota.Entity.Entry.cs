namespace TrialUsersMediator;

public partial record TrialUser
{
    public partial record Quota<EQuota>
        where EQuota : struct, Enum
    {
        public partial record Entity
        {
            public partial record Entry(EQuota Type, int Value)
            {
                public int Id { get; private set; } = default!;
                required public Entity Quota { get; set; } = default!;
            }
        }
    }
}
