using Node.Listeners;

namespace Node;

public class AuthenticatedTarget
{
    public required LocalListener LocalListener { get; init; }

    readonly ILogger Logger;

    public AuthenticatedTarget(ILogger<AuthenticatedTarget> logger) => Logger = logger;

    public async Task Execute()
    {
        if (Settings.SessionId is not null)
            Logger.Info($"Session ID is present. Email: {Settings.Email ?? "<not saved>"}; User ID: {Settings.UserId}; {(Settings.IsSlave == true ? "slave" : "non-slave")}");
        else
        {
            await WaitForAuth().ConfigureAwait(false);
            Logger.Info("Authentication completed");
        }
    }

    async ValueTask WaitForAuth()
    {
        Logger.Warn(@$"You are not authenticated. Please use NodeUI app to authenticate or create an 'login' file with username and password separated by newline");

        while (true)
        {
            await Task.Delay(1000).ConfigureAwait(false);
            if (File.Exists("login"))
            {
                var data = File.ReadAllText("login").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (data.Length < 2) continue;

                var login = data[0];
                var password = data[1];

                var auth = await SessionManager.AuthAsync(login, password);
                auth.LogIfError();
                if (!auth) continue;

                return;
            }

            if (Settings.SessionId is null) continue;
            if (Settings.NodeName is null) continue;
            if (Settings.Guid is null) continue;
            if (Settings.UserId is null) continue;

            return;
        }
    }
}
