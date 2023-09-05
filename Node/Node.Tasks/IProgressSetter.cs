namespace Node.Tasks;

public interface IProgressSetter
{
    /// <summary> Set progress </summary>
    /// <param name="progress"> Progress value, 0-1 </param>
    void Set(double progress);
}
