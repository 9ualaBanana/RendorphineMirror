namespace Transport.Upload._3DModelsUpload.Models.CGTrader;

public class CGTrader3DModelMetadata : _3DModelMetadata
{
    const double DefaultPrice = 0.0;

    public string Title { get; }
    public string Description { get; }
    public string[] Tags
    {
        get => _tags!;
        private init
        {
            if (value.Length < 5) throw new ArgumentOutOfRangeException(
                nameof(value.Length),
                value.Length,
                $"{nameof(CGTrader3DModelMetadata)} requires at least 5 tags.");

            _tags = value;
        }
    }
    string[]? _tags;
    public string Category { get; }
    public string SubCategory { get; }
    public CGTraderLicense License { get; }
    public string? CustomLicense { get; }
    public double Price { get; }
    public ProductType ProductType { get; }
    public bool? GameReady { get; }
    public bool? Animated { get; }
    public bool? Rigged { get; }
    public AdditionalInfo? Info { get; }

    #region Initialization

    public static CGTrader3DModelMetadata ForCG(
     string title,
     string description,
     string[] tags,
     string category,
     string subCategory,
     NonCustomCGTraderLicense license,
     double price = DefaultPrice,
     bool gameReady = false,
     bool animated = false,
     bool rigged = false,
     AdditionalInfo? info = null) => new(
         title,
         description,
         tags,
         category,
         subCategory,
         Enum.Parse<CGTraderLicense>(license.ToString()),
         price,
         customLicenseText: null,
         ProductType.cg,
         gameReady,
         animated,
         rigged,
         info);

    public static CGTrader3DModelMetadata ForCG(
        string title,
        string description,
        string[] tags,
        string category,
        string subCategory,
        string customLicenseText,
        double price = DefaultPrice,
        bool gameReady = false,
        bool animated = false,
        bool rigged = false,
        AdditionalInfo? info = null) => new(
            title,
            description,
            tags,
            category,
            subCategory,
            CGTraderLicense.custom,
            price,
            customLicenseText,
            ProductType.cg,
            gameReady,
            animated,
            rigged,
            info);

    public static CGTrader3DModelMetadata ForPrintable(
        string title,
        string description,
        string[] tags,
        string category,
        string subCategory,
        NonCustomCGTraderLicense license,
        double price = DefaultPrice) => new(
            title,
            description,
            tags,
            category,
            subCategory,
            Enum.Parse<CGTraderLicense>(license.ToString()),
            price,
            customLicenseText: null,
            ProductType.printable);

    public static CGTrader3DModelMetadata ForPrintable(
        string title,
        string description,
        string[] tags,
        string category,
        string subCategory,
        string customLicenseText,
        double price = DefaultPrice) => new(
            title,
            description,
            tags,
            category,
            subCategory,
            CGTraderLicense.custom,
            price,
            customLicenseText,
            ProductType.printable);

    CGTrader3DModelMetadata(
        string title,
        string description,
        string[] tags,
        string category,
        string subCategory,
        CGTraderLicense license,
        double price = 0.0,
        string? customLicenseText = null,
        ProductType productType = ProductType.cg,
        bool? gameReady = null,
        bool? animated = null,
        bool? rigged = null,
        AdditionalInfo? info = null)
    {
        Title = title;
        Description = description;
        Tags = tags;
        Category = category;
        SubCategory = subCategory;
        License = license;
        Price = price;
        CustomLicense = customLicenseText;
        ProductType = productType;
        GameReady = gameReady;
        Animated = animated;
        Rigged = rigged;
        Info = info;
    }

    #endregion
}

public enum NonCustomCGTraderLicense { royalty_free, editorial }

public enum CGTraderLicense { royalty_free, editorial, custom }

public enum ProductType { cg, printable }
