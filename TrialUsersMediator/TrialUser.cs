namespace TrialUsersMediator;

public class TrialUser
{
    internal long Identifier { get; private set; }

    internal Platform Platform { get; private set; }

    internal static Builder From(Platform platform)
        => Builder.ForClientFrom(platform);


    internal class Builder
    {
        readonly TrialUser _client;

        internal static Builder ForClientFrom(Platform platform)
            => new(platform);

        Builder(Platform platform)
        { _client = new() { Platform = platform }; }

        internal TrialUser With(long identifier)
        {
            _client.Identifier = identifier;

            ArgumentNullException.ThrowIfNull(_client.Platform, nameof(_client.Platform));
            return _client;
        }
    }
}
