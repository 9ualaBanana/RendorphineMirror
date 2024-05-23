using Microsoft.AspNetCore.Mvc;

namespace Node.Listeners;

[ApiController]
[Route("rphtaskexec")]
public class TaskReceiverController : ControllerBase
{
    readonly ILogger<TaskReceiverController> Logger;

    public TaskReceiverController(ILogger<TaskReceiverController> logger) => Logger = logger;

    [HttpPost("launchtask")]
    public async Task<ActionResult> LaunchTask([FromForm] string taskid, [FromForm] string task, [FromForm] string tlhost,
        [FromServices] IQueuedTasksStorage queuedTasks, [FromServices] Notifier notifier)
    {
        if (!Settings.AcceptTasks.Value)
            return NotFound();

        Logger.Info($"@rphtaskexec/launchtask received {taskid} tlhost={tlhost} {task}");

        var taskinfo = JsonConvert.DeserializeObject<TaskInfo>(task)!;
        Logger.Info($"Received a new task: id: {taskid}; data {task}");

        queuedTasks.QueuedTasks.Add(new ReceivedTask(taskid, taskinfo) { HostShard = tlhost });
        notifier.Notify($"Received task {taskid}\n ```json\n{JsonConvert.SerializeObject(task, JsonSettings.LowercaseIgnoreNull):n}\n```");

        return Ok("{\"ok\":1}");
    }
}
