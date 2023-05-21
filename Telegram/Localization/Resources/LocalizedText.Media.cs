using Microsoft.Extensions.Localization;

namespace Telegram.Localization.Resources;

public abstract partial class LocalizedText
{
    public class Media : LocalizedText
    {
        public Media(IStringLocalizer<Media> localizer)
            : base(localizer)
        {
        }

        internal string ChooseHowToProcess => Localizer[nameof(ChooseHowToProcess)];

        internal string SpecifyExtensionAsCaption => Localizer[nameof(SpecifyExtensionAsCaption)];

        internal string Uploading => Localizer[nameof(Uploading)];

        internal string UploadSucceeded => Localizer[nameof(UploadSucceeded)];

        internal string UploadFailed => Localizer[nameof(UploadFailed)];

        internal string UploadButton => Localizer[nameof(UploadButton)];

        internal string UpscaleButton => Localizer[nameof(UpscaleButton)];

        internal string VectorizeButton => Localizer[nameof(VectorizeButton)];

        internal string ResultPromise => Localizer[nameof(ResultPromise)];

        internal string Expired => Localizer[nameof(Expired)];
    }
}
