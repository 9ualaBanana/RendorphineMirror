using _3DProductsPublish.Turbosquid._3DModelComponents;
using System.Net.Http.Json;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

internal partial class TurboSquid3DProductAssetsProcessing
{
    abstract class Payload
    {
        readonly string upload_key;
        readonly string resource;
        readonly string draft_id;
        readonly string name;
        readonly long size;
        readonly string authenticity_token;

        internal static Payload For(FileStream archived3DModel, string uploadKey, TurboSquid3DProductUploadSessionContext uploadSessionContext, string fileFormat, string formatVersion, string renderer, string? rendererVersion, bool isNative)
            => For(archived3DModel, uploadKey, uploadSessionContext.ProductDraft._ID, uploadSessionContext.Credential._CsrfToken, fileFormat, formatVersion, renderer, rendererVersion, isNative);
        internal static Payload For(FileStream archived3DModel, string uploadKey, string draftId, string authenticityToken, string fileFormat, string formatVersion, string renderer, string? rendererVersion, bool isNative)
            => new _3DModel(uploadKey, draftId, archived3DModel.Name, archived3DModel.Length, authenticityToken, fileFormat, formatVersion, renderer, rendererVersion, isNative);

        internal static Payload For(TurboSquid3DProductThumbnail thumbnail, string uploadKey, TurboSquid3DProductUploadSessionContext uploadSessionContext)
            => For(thumbnail, uploadKey, uploadSessionContext.ProductDraft._ID, uploadSessionContext.Credential._CsrfToken);
        internal static Payload For(TurboSquid3DProductThumbnail thumbnail, string uploadKey, string draftId, string authenticityToken)
            => new Thumbnail(uploadKey, draftId, thumbnail.FileName, thumbnail.Size, thumbnail.Type.ToString(), authenticityToken);

        internal Payload(
            string uploadKey,
            string resource,
            string draftId,
            string name,
            long size,
            string authenticityToken)
        {
            upload_key = uploadKey;
            this.resource = resource;
            draft_id = draftId;
            this.name = Path.GetFileName(name);
            this.size = size;
            authenticity_token = authenticityToken;
        }

        internal JsonContent ToJson() => JsonContent.Create(new
        {
            upload_key,
            resource,
            attributes,
            authenticity_token
        });
        protected abstract object attributes { get; }


        internal class _3DModel : Payload
        {
            readonly string file_format;
            readonly string format_version;
            readonly string renderer;
            readonly string? renderer_version;
            readonly bool is_native;

            internal _3DModel(
                string uploadKey,
                string draftId,
                string name,
                long size,
                string authenticityToken,
                string fileFormat,
                string formatVersion,
                string renderer,
                string? rendererVersion,
                bool isNative)
                : base(uploadKey, "product_files", draftId, name, size, authenticityToken)
            {
                file_format = fileFormat;
                format_version = formatVersion;
                this.renderer = renderer;
                renderer_version = rendererVersion;
                is_native = isNative;
            }

            protected override object attributes => new
            {
                draft_id,
                name,
                size,
                file_format,
                format_version,
                renderer,
                renderer_version,
                is_native
            };
        }

        internal class Thumbnail : Payload
        {
            readonly string thumbnail_type;

            public Thumbnail(
                string uploadKey,
                string draftId,
                string name,
                long size,
                string thumbnailType,
                string authenticityToken)
                : base(uploadKey, "thumbnails", draftId, name, size, authenticityToken)
            {
                thumbnail_type = thumbnailType;
            }

            protected override object attributes => new
            {
                draft_id,
                name,
                size,
                thumbnail_type,
                watermarked = false
            };
        }
    }
}