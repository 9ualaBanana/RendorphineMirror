namespace TrialUsersMediator.Persistence;

public partial record TrialUser
{
    internal long Identifier { get; private set; } = default!;

    internal Platform Platform { get; private set; } = default!;

    internal static Builder From(Platform platform)
        => Builder.For(new TrialUser() { Platform = platform });


    internal class Builder
    {
        readonly TrialUser _trialUser;

        internal static Builder For(TrialUser trialUser)
            => new(trialUser);
        protected Builder(TrialUser trialUser)
        { _trialUser = trialUser; }

        internal TrialUser With(long identifier)
        { _trialUser.Identifier = identifier; return ValidatedProduct; }

        protected virtual TrialUser ValidatedProduct
        {
            get
            {
                ArgumentNullException.ThrowIfNull(_trialUser.Platform, nameof(_trialUser.Platform));

                return _trialUser;
            }
        }
    }
}
