using System.Xml.Linq;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload.Requests;

internal record struct MultipartAssetUploadResult(IEnumerable<AssetPartUploadResult> _AssetPartUploadResults)
{
    internal string _ToXML()
    {
        XNamespace xmlns = "http://s3.amazonaws.com/doc/2006-03-01/";
        return new XElement(xmlns + "CompleteMultipartUpload",
            _AssetPartUploadResults.Select(p => p._ToXElementIn(xmlns))
            ).ToString();
    }
}
