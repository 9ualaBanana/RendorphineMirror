namespace MarkTM.RFProduct;

public static class RFProductExtensions
{
    public static void DeleteProductsWithContainerAt(this IRFProductStorage products, string path)
    {
        // toarray is nesessary
        foreach (var product in products.GetProductsWithContainerAt(path).ToArray())
            products.RFProducts.Remove(product.ID);
    }
    public static IEnumerable<RFProduct> GetProductsWithContainerAt(this IRFProductStorage products, string path)
    {
        path = Path.GetFullPath(path);

        return products.RFProducts.Values
            .Where(product => Path.GetFullPath(product.Path) != path);
    }

    public static IEnumerable<RFProduct> GetSubProductsRecursive(this RFProduct product) => product.SubProducts.SelectMany(GetSubProductsRecursive).Append(product);

    /// <summary> Remove a product from the DB with all its subproducts </summary>
    public static void RemoveProductRecirsive(this IRFProductStorage products, RFProduct parent, bool deleteFiles)
    {
        foreach (var product in parent.GetSubProductsRecursive())
        {
            products.RFProducts.Remove(product);

            if (deleteFiles)
                product.Delete();
        }
    }
}
