namespace Node;

public class RFProductStorage : IRFProductStorage
{
    public DatabaseValueDictionary<string, RFProduct> RFProducts { get; }

    public RFProductStorage(DataDirs dirs)
    {
        var db = new Database(dirs.DataFile("rfproducts.db"));
        RFProducts = new DatabaseValueDictionary<string, RFProduct>(db, nameof(RFProducts), p => p.ID);
    }
}
