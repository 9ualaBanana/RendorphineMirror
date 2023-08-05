namespace TrialUsersMediator;

public partial record TrialUser
{
    required public long Identifier { get; set; } = default!;

    required public Platform Platform { get; set; } = default!;
}
