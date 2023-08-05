using Microsoft.Extensions.Localization;

namespace Telegram.Localization.Resources;

public abstract partial class LocalizedText
{
    protected readonly IStringLocalizer Localizer;

	protected LocalizedText(IStringLocalizer localizer)
	{
		Localizer = localizer;
	}
}
