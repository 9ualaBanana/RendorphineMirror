using System.Net;
using System.Text;

namespace Node;

public class SessionManager
{
    const string Endpoint = Api.TaskManagerEndpoint;

    public required Api Api { get; init; }
    public required ILogger<SessionManager> Logger { get; init; }

    public bool IsLoggedIn() =>
        Settings.SessionId is not null
        && Settings.NodeName is not null
        && Settings.Guid is not null
        && Settings.UserId is not null;

    public ValueTask<OperationResult> RenameServerAsync(string newname, string oldname) =>
        Api.ApiPost($"{Endpoint}/renameserver", "Renaming the node", ("sessionid", Settings.SessionId), ("oldname", oldname), ("newname", newname));

    public ValueTask<OperationResult> AutoAuthAsync(string email) => AutoAuthAsync(email, Guid.NewGuid().ToString());
    public ValueTask<OperationResult> AutoAuthAsync(string email, string guid) =>
        Api.ApiPost<LoginResult>($"{Endpoint}/autologin", null, "Autologging in", ("email", email), ("guid", guid))
        .Next(res => LoginSuccess(res.SessionId, email, guid, res.UserId, true));

    public ValueTask<OperationResult> AuthAsync(string email, string password) => AuthAsync(email, password, Guid.NewGuid().ToString());
    public ValueTask<OperationResult> AuthAsync(string email, string password, string guid) =>
        Api.ApiPost<LoginResult>($"{Endpoint}/login", null, "Logging in", ("email", email), ("password", password), ("guid", guid))
        .Next(res => LoginSuccess(res.SessionId, email, guid, res.UserId, false));

    public async ValueTask<OperationResult> WebAuthAsync(CancellationToken token = default)
    {
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:3525/rphtaskexec/mpoauthresult/");
        listener.Start();

        token.Register(listener.Close);

        var guid = Guid.NewGuid().ToString();
        Process.Start(new ProcessStartInfo($"{Api.TaskManagerEndpoint}/mpoauthloginnode?guid={guid}") { UseShellExecute = true }).ThrowIfNull();

        while (true)
        {
            try
            {
                if (token.IsCancellationRequested) return false;
                var context = await listener.GetContextAsync();
                using var response = context.Response;

                if (token.IsCancellationRequested) return false;

                var request = context.Request;
                var query = request.QueryString;
                var sid = query["sessionid"].ThrowIfNull();
                var uid = query["userid"].ThrowIfNull();

                var login = await LoginSuccess(sid, null, guid, uid, false);

                response.StatusCode = (int) HttpStatusCode.OK;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("<html><body>Authentication successful, you can now close this page</body></html>"), token);

                return login;
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception ex) { LogManager.GetCurrentClassLogger().Error(ex); }
        }
    }

    async ValueTask<OperationResult> LoginSuccess(string sid, string? email, string guid, string userid, bool slave)
    {
        Settings.AuthInfo = new AuthInfo(sid, email, guid, userid, slave);
        if (string.IsNullOrEmpty(Settings.NodeName))
        {
            var nickr = await Api.ApiPost<string>($"{Endpoint}/generatenickname", "nickname", "Generating nickname", ("sessionid", Settings.SessionId));
            nickr.LogIfError();

            if (nickr) Settings.NodeName = nickr.Value;
            else Settings.NodeName = email + "_" + guid;

            Logger.LogInformation($"Generated nickname: {Settings.NodeName ?? "!!NULL!!"}");
        }

        return true;
    }


    public record LoginResult(string SessionId, string UserId, AccessLevel AccessLevel);
    public enum AccessLevel { User, Level1, Level2, Leve3, Level4, Level5 }
}
