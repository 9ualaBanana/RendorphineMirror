using System.Text;
using System.Xml.Linq;

namespace Transport.Upload._3DModelsUpload.Turbosquid.Upload;

internal record struct AssetMultipartUploadResult(IEnumerable<AssetPartUploadResult> _PartUploadResults)
{
    internal string _ToXML()
    {
        XNamespace xmlns = "http://s3.amazonaws.com/doc/2006-03-01/";
        return new XElement(xmlns + "CompleteMultipartUpload",
            _PartUploadResults.Select(p => p._ToXElementIn(xmlns))
            ).ToString();
    }
}
