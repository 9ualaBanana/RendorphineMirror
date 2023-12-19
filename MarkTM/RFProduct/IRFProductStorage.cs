using Node.Common;

namespace MarkTM.RFProduct;

public interface IRFProductStorage
{
    DatabaseValueDictionary<string, RFProduct> RFProducts { get; }
}
