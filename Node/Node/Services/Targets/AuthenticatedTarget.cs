using Node.Heartbeat;
using Node.Listeners;
using Node.Profiling;

namespace Node.Services.Targets;

public class AuthenticatedTarget : IServiceTarget
{
    public static void CreateRegistrations(ContainerBuilder builder) { }

    public required UITarget UI { get; init; }
    public required LocalListener LocalListener { get; init; }
    public required SessionManager SessionManager { get; init; }
    public required SettingsInstance Settings { get; init; }
    public required Init Init { get; init; }
    public required ILogger<AuthenticatedTarget> Logger { get; init; }

    public required Api Api { get; init; }

    async Task IServiceTarget.ExecuteAsync()
    {
        if (Settings.SessionId is null)
        {
            await WaitForAuth().ConfigureAwait(false);
            await MPlusHeartbeat.Send(Api, await Profiler.CreateDummyAsync(Init.Version, Settings))
                .ThrowIfError();

            Logger.Info("Authentication completed");
        }

        Logger.Info($"Email: {Settings.Email ?? "<not saved>"}; User ID: {Settings.UserId}; Guid: {Settings.Guid}; {(Settings.IsSlave == true ? "slave" : "non-slave")}");
    }

    async ValueTask WaitForAuth()
    {
        Logger.Warn(@$"You are not authenticated. Please use NodeUI app to authenticate or create a 'login' file with username and password separated by newline");

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

            if (!SessionManager.IsLoggedIn())
                continue;

            return;
        }
    }
}
