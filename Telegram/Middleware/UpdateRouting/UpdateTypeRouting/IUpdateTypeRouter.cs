﻿using Telegram.Bot.Types;

namespace Telegram.Middleware.UpdateRouting.UpdateTypeRouting;

/// <summary>
/// Service type of <see cref="ISwitchableMiddleware{TMiddleware, TSwitch}"/> invoked by <see cref="UpdateTypeRouterMiddleware"/>.
/// </summary>
public interface IUpdateTypeRouter : ISwitchableMiddleware<IUpdateTypeRouter, Update>
{
}
