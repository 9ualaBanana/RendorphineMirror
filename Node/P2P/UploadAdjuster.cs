namespace Node.P2P;

internal class UploadAdjuster
{
    readonly int _batchSizeLimit;
    TimeSpan lastUploadTime = TimeSpan.Zero;

    internal UploadAdjuster(int batchSizeLimit)
    {
        _batchSizeLimit = batchSizeLimit;
    }

    internal void Adjust(ref int packetSize, ref int batchSize, TimeSpan uploadTime)
    {
        if (lastUploadTime == TimeSpan.Zero)
        {
            lastUploadTime = uploadTime;
            return;
        }

        var uploadTimeDecreaseDegree = lastUploadTime.Divide(uploadTime);
        if (uploadTimeDecreaseDegree >= 1.5)
        {
            packetSize *= (int)Math.Round(uploadTimeDecreaseDegree);
            if (batchSize < _batchSizeLimit) batchSize *= 2;
            lastUploadTime = uploadTime;
        }
    }
}
