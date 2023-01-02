using System.Xml.Linq;

namespace _3DProductsPublish.Turbosquid.Upload.Requests;

internal record struct AssetPartUploadResult(int PartNumber, string ETag)
{
    internal XElement _ToXElementIn(XNamespace ns) =>
        new(ns + "Part",
            new XElement(ns + "ETag", ETag),
            new XElement(ns + "PartNumber", PartNumber)
            );
}
