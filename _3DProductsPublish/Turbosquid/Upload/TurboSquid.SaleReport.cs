using System.Globalization;

namespace _3DProductsPublish.Turbosquid.Upload;

public partial class TurboSquid
{
    public record SaleReport
    {   
        internal class Uri
        {
            const int ID = 20;
            internal static System.Uri For(int month, int year)
                // `xsl` argument defines a format of the requested resource: 0 - webpage; 1 - .csv file.
                => new UriBuilder { Scheme = "https", Host = "www.turbosquid.com", Path = "Report/Index.cfm", Query = $"report_id={ID}&xsl=0&theyear={year}&theMonth={month}" }.Uri;
        }
    }
}
