namespace Common.Tasks.Tasks;

public class Crop
{
    public int X, Y, W, H;

    public Crop(int x, int y, int w, int h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }
}
public record MediaEditInfo : IPluginActionData<MediaEditInfo>
{
    public Crop Crop;
    public double Bri, Sat, Con, Gam;
    public bool Hflip, Vflip;
    public double Ro;

    public MediaEditInfo(Crop crop, double bri, double sat, double con, double gam, bool hflip, bool vflip, double ro)
    {
        Crop = crop;
        Bri = bri;
        Sat = sat;
        Con = con;
        Gam = gam;
        Hflip = hflip;
        Vflip = vflip;
        Ro = ro;
    }

    public static async ValueTask<MediaEditInfo> CreateDefault(CreateTaskData data)
    {
        await Task.Yield();
        // TODO; get file info and then blalbalbalblabla

        return new(new Crop(1, 2, 5, 6), 1, 1, 1, 1, false, false, 0);
    }
}