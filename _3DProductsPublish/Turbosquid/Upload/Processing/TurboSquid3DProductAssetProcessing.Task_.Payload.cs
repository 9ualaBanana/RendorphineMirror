using _3DProductsPublish._3DProductDS;
using _3DProductsPublish.Turbosquid._3DModelComponents;
using System.Net.Http.Json;

namespace _3DProductsPublish.Turbosquid.Upload.Processing;

internal partial class TurboSquid3DProductAssetProcessing
{
    internal partial class Task_<TAsset>
    {
        abstract class Payload
        {
            readonly string upload_key;
            readonly string resource;
            readonly string draft_id;
            readonly string name;
            readonly long size;
            internal readonly string authenticity_token;

            internal static Payload For(TAsset asset, string uploadKey, TurboSquid.PublishSession session)
                => asset switch
                {
                    _3DModel<TurboSquid3DModelMetadata> _3DModel => Payload.For(_3DModel, uploadKey, session),
                    _3DProductThumbnail thumbnail => Payload.For(thumbnail, uploadKey, session),
                    _3DProduct.Texture_ texture => Payload.For(texture, uploadKey, session),
                    _ => throw new NotImplementedException()
                };

            static Payload For(_3DModel<TurboSquid3DModelMetadata> _3DModel, string uploadKey, TurboSquid.PublishSession session)
                => Payload.For(_3DModel, uploadKey, session.Draft.ID, session.Client.Credential.AuthenticityToken);
            static Payload For(_3DModel<TurboSquid3DModelMetadata> _3DModel, string uploadKey, string draftId, string authenticityToken)
            {
                using var archived3DModel = File.OpenRead(_3DModel.Archive().Result);
                return new Payload._3DModel(uploadKey, draftId, archived3DModel.Name, archived3DModel.Length, authenticityToken,
                    _3DModel.Metadata.FileFormat, _3DModel.Metadata.FormatVersion, _3DModel.Metadata.Renderer, _3DModel.Metadata.RendererVersion, _3DModel.Metadata.IsNative);
            }

            static Payload For(_3DProductThumbnail thumbnail, string uploadKey, TurboSquid.PublishSession session)
                => Payload.For(thumbnail, uploadKey, session.Draft.ID, session.Client.Credential.AuthenticityToken);
            static Payload For(_3DProductThumbnail thumbnail, string uploadKey, string draftId, string authenticityToken)
                => new Payload.Thumbnail(uploadKey, draftId, thumbnail.FileName, thumbnail.Size, thumbnail.TurboSquidType().ToString(), authenticityToken);

            static Payload For(_3DProduct.Texture_ texture, string uploadKey, TurboSquid.PublishSession session)
                => Payload.For(texture, uploadKey, session.Draft.ID, session.Client.Credential.AuthenticityToken);
            static Payload For(_3DProduct.Texture_ texture, string uploadKey, string draftId, string authenticityToken)
                => new Payload.Texture(uploadKey, draftId, texture.Name, texture.Size, authenticityToken);

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
                readonly string? renderer;
                readonly string? renderer_version;
                readonly bool is_native;

                internal _3DModel(
                    string uploadKey,
                    string draftId,
                    string name,
                    long size,
                    string authenticityToken,
                    string fileFormat,
                    double formatVersion,
                    string? renderer,
                    double? rendererVersion,
                    bool isNative)
                    : base(uploadKey, "product_files", draftId, name, size, authenticityToken)
                {
                    file_format = fileFormat;
                    format_version = formatVersion.ToString();
                    this.renderer = renderer;
                    renderer_version = rendererVersion?.ToString();
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

                internal Thumbnail(
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

            internal class Texture : Payload
            {
                internal Texture(
                    string uploadKey,
                    string draftId,
                    string name,
                    long size,
                    string authenticityToken)
                    : base(uploadKey, "texture_files", draftId, name, size, authenticityToken)
                {
                }

                protected override object attributes => new
                {
                    draft_id,
                    name,
                    size
                };
            }
        }
    }
}