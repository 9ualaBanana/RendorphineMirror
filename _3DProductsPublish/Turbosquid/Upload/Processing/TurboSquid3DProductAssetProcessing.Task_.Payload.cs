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

            internal static Payload For(I3DProductAsset asset, string uploadKey, TurboSquid.PublishSession session)
                => asset switch
                {
                    _3DProductDS._3DModel _3DModel => Payload.For(_3DModel, uploadKey, session),
                    _3DProductThumbnail thumbnail => Payload.For(thumbnail, uploadKey, session),
                    _3DProduct.Textures_ textures => Payload.For(textures, uploadKey, session),
                    _ => throw new NotImplementedException()
                };

            static Payload._3DModel For(_3DProductDS._3DModel _3DModel, string uploadKey, TurboSquid.PublishSession session)
                => Payload.For(_3DModel, session._3DProduct.Tracker.Model(_3DModel)._, uploadKey, session._3DProduct.Tracker.Data.DraftID, session.Client.Credential.AuthenticityToken);
            static Payload._3DModel For(_3DProductDS._3DModel _3DModel, TurboSquid3DModelMetadata metadata, string uploadKey, long draftId, string authenticityToken)
            {
                using var archived3DModel = File.OpenRead(_3DModel.Archived);
                return new Payload._3DModel(uploadKey, draftId, archived3DModel.Name, archived3DModel.Length, authenticityToken,
                    metadata.FileFormat, metadata.FormatVersion, metadata.Renderer, metadata.RendererVersion, metadata.IsNative);
            }

            static Payload.Thumbnail For(_3DProductThumbnail thumbnail, string uploadKey, TurboSquid.PublishSession session)
                => Payload.For(thumbnail, uploadKey, session._3DProduct.Tracker.Data.DraftID, session.Client.Credential.AuthenticityToken);
            static Payload.Thumbnail For(_3DProductThumbnail thumbnail, string uploadKey, long draftId, string authenticityToken)
                => new Payload.Thumbnail(uploadKey, draftId, thumbnail.Name(), thumbnail.Size(), TurboSquidProcessed3DProductThumbnail.PreprocessedType(thumbnail).ToString(), authenticityToken);

            static Payload.Texture For(_3DProduct.Textures_ texture, string uploadKey, TurboSquid.PublishSession session)
                => Payload.For(texture, uploadKey, session._3DProduct.Tracker.Data.DraftID, session.Client.Credential.AuthenticityToken);
            static Payload.Texture For(_3DProduct.Textures_ textures, string uploadKey, long draftId, string authenticityToken)
                => new Payload.Texture(uploadKey, draftId, textures.Name(), textures.Size(), authenticityToken);

            internal Payload(
                string uploadKey,
                string resource,
                long draftId,
                string name,
                long size,
                string authenticityToken)
            {
                upload_key = uploadKey;
                this.resource = resource;
                draft_id = draftId.ToString();
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
                    long draftId,
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
                    long draftId,
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
                    long draftId,
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