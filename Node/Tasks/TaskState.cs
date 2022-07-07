﻿namespace Node.Tasks;

public enum TaskState
{
    Queued,
    Input,
    Active,
    Output,
    Finished,
    Canceled,
    Failed,
}
