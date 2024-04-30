﻿using Node.Common.Models;
using Node.Tasks.Exec.Output;
using Node.Tasks.Models;

namespace MarkTM.RFProduct;

public partial record RFProduct
{
    public record Image : RFProduct
    {
        public record Constructor : Constructor<Idea_, QSPreviews, Image>
        {
            internal override Image Create(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
                => new(idea, id, previews, container);
        }
        Image(Idea_ idea, string id, QSPreviews previews, AssetContainer container)
            : base(idea, id, previews, container)
        {
        }


        new public record Idea_
            : RFProduct.Idea_
        {
            Idea_(string path)
                : base(path)
            {
            }
            public record Recognizer : IRecognizer<Idea_>
            {
                public Idea_ Recognize(string idea) =>
                    File.Exists(idea) ?
                        FileFormatExtensions.FromFilename(idea) is FileFormat.Jpeg or FileFormat.Png ?

                    new(idea)

                        : throw new FormatException()
                    : throw new FileNotFoundException(default, idea);
            }
        }

        new public record QSPreviews(
            [property: JsonProperty(nameof(QSPreviewOutput.ImageFooter))] FileWithFormat ImageWithFooter,
            [property: JsonProperty(nameof(QSPreviewOutput.ImageQr))] FileWithFormat ImageWithQR) : RFProduct.QSPreviews
        {
            public record Generator : Generator<QSPreviews>;

            public override IEnumerator<FileWithFormat> GetEnumerator()
                => new[] { ImageWithFooter, ImageWithQR }.AsEnumerable().GetEnumerator();
        }
    }
}