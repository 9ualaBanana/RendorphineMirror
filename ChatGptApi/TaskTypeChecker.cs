namespace ChatGptApi;

public static class TaskTypeChecker
{
    public static async Task ThrowIfTaskTypeNotValid(TaskAction action, string sessionid, string taskid)
    {
        if (await IsTaskTypeValid(action, sessionid, taskid))
            return;

        throw new Exception("no");
    }
    public static async Task<bool> IsTaskTypeValid(TaskAction action, string sessionid, string taskid)
    {
        // TODO: remove after testing
        if (sessionid == "63fe288368974192c27a5388")
            return true;

        var state = (await Apis.DefaultWithSessionId(sessionid).GetTaskStateAsync(new TaskApi(taskid)))
            .ThrowIfError("").ThrowIfNull("");

        return state?.Type == action;
    }
}
