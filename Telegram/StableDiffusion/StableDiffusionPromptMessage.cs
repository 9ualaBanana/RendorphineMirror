namespace Telegram.StableDiffusion;

/// <summary>
/// Stable Diffusion prompt received inside the <see cref="Bot.Types.Message"/>.
/// </summary>
/// <param name="Prompt">The normalized prompt received inside the <see cref="Message"/>.</param>
/// <param name="Message">The message that contains the unnormalized Stable Diffusion prompt.</param>
internal record StableDiffusionPromptMessage(string Prompt, Message Message)
{
}
