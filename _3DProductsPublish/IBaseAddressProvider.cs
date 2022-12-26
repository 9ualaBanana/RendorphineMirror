namespace _3DProductsPublish;

internal interface IBaseAddressProvider
{
    string Endpoint(string? path = null)
    {
        path ??= string.Empty;
        if (path.StartsWith('/')) path = path.TrimStart('/');

        return $"{Path.TrimEndingDirectorySeparator(BaseAddress)}/{path}";
    }

    string BaseAddress { get; }
}
