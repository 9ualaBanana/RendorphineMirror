using Node.Listeners;

namespace Node.Services.Targets;

public class AuthenticatedTarget : IServiceTarget
{
    public required LocalListener LocalListener { get; init; }
    public required SessionManager SessionManager { get; init; }
    public required ILogger<AuthenticatedTarget> Logger { get; init; }

    public static void CreateRegistrations(ContainerBuilder builder)
    {

    }

    public async Task ExecuteAsync()
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
