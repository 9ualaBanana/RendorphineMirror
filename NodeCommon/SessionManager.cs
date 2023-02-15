﻿using System.Diagnostics;
using System.Net;
using System.Text;

namespace NodeCommon;

public static class SessionManager
{
    const string Endpoint = Api.TaskManagerEndpoint;
    public static string SessionId = null!;


    public static ValueTask<OperationResult> RenameServerAsync(string name) =>
        Api.Default.ApiPost($"{Endpoint}/renameserver", "Renaming the node", ("sessionid", SessionId), ("oldname", Settings.NodeName), ("newname", name));

    public static ValueTask<OperationResult> AutoAuthAsync(string email) => AutoAuthAsync(email, Guid.NewGuid().ToString());
    public static ValueTask<OperationResult> AutoAuthAsync(string email, string guid) =>
        Api.Default.ApiPost<LoginResult>($"{Endpoint}/autologin", null, "Autologging in", ("email", email), ("guid", guid))
        .Next(res => LoginSuccess(res.SessionId, email, guid, res.UserId, true));

    public static ValueTask<OperationResult> AuthAsync(string email, string password) => AuthAsync(email, password, Guid.NewGuid().ToString());
    public static ValueTask<OperationResult> AuthAsync(string email, string password, string guid) =>
        Api.Default.ApiPost<LoginResult>($"{Endpoint}/login", null, "Logging in", ("email", email), ("password", password), ("guid", guid))
        .Next(res => LoginSuccess(res.SessionId, email, guid, res.UserId, false));

    public static async ValueTask<OperationResult> WebAuthAsync(CancellationToken token = default)
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
                var context = await listener.GetContextAsync().ConfigureAwait(false);
                using var response = context.Response;

                if (token.IsCancellationRequested) return false;

                var request = context.Request;
                var query = request.QueryString;
                var sid = query["sessionid"].ThrowIfNull();
                var uid = query["userid"].ThrowIfNull();

                var login = await LoginSuccess(sid, null, guid, uid, false).ConfigureAwait(false);

                response.StatusCode = (int) HttpStatusCode.OK;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("<html><body>Authentication successful, you can now close this page</body></html>")).ConfigureAwait(false);

                return login;
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception ex) { LogManager.GetCurrentClassLogger().Error(ex); }
        }
    }

    static async ValueTask<OperationResult> LoginSuccess(string sid, string? email, string guid, string userid, bool slave)
    {
        Settings.AuthInfo = new AuthInfo(sid, email, guid, userid, slave);
        if (Settings.NodeName is null)
        {
            var nickr = await RequestNicknameAsync().ConfigureAwait(false);
            nickr.LogIfError();

            if (nickr) Settings.NodeName = nickr.Value;
            else Settings.NodeName = email + "_" + guid;
        }

        return true;


        static ValueTask<OperationResult<string>> RequestNicknameAsync() =>
            Api.Default.ApiPost<string>($"{Endpoint}/generatenickname", "nickname", "Generating nickname", ("sessionid", SessionId));
    }


    record LoginResult(string SessionId, string UserId, AccessLevel AccessLevel);
    enum AccessLevel { User, Level1, Level2, Leve3, Level4, Level5 }
}
