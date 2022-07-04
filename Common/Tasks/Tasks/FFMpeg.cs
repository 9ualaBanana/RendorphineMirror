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
public abstract class MediaEditInfo : IPluginActionData
{
    public Crop? Crop;
    public double? Bri, Sat, Con, Gam;
    public bool? Hflip, Vflip;
    public double? Ro;


    public virtual string ConstructFFMpegArguments()
    {
        var filters = new List<string>();

        if (Crop is not null) filters.Add($"\"crop={Crop.W}:{Crop.H}:{Crop.X}:{Crop.Y}\"");

        // TODO:

        if (Hflip == true) filters.Add("hflip");
        if (Vflip == true) filters.Add("vflip");

        return string.Join(' ', filters.Select(x => "-vf " + x));
    }
}
public class EditVideoInfo : MediaEditInfo
{
    public double? CutFrameAt;

    public override string ConstructFFMpegArguments()
    {
        var args = base.ConstructFFMpegArguments();

        // TODO:

        return args;
    }
}
public class EditRasterInfo : MediaEditInfo
{
}