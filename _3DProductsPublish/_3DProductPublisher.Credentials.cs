using System.Net;

namespace _3DProductsPublish;

public partial class _3DProductPublisher
{
    public record Credentials(
        NetworkCredential CGTrader,
        NetworkCredential TurboSquid);
}
