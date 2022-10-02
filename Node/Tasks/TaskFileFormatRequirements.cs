using System.Collections;

namespace Node.Tasks;

public class TaskFileFormatRequirements : IEnumerable<TaskFileFormatRequirements.IRequirement>
{
    readonly List<IRequirement> Requirements = new();

    public TaskFileFormatRequirements() { }
    public TaskFileFormatRequirements(FileFormat singleRequired) => RequiredOne(singleRequired);


    public OperationResult Check(ReceivedTask task) => Requirements.Select(x => x.Check(task)).Merge();
    public IEnumerable<FileWithFormat> GetInputFiles(ReceivedTask task)
    {
        var inputs = task.InputFiles.ToList(); // copy
        var result = new List<FileWithFormat>();

        foreach (var req in Requirements)
            req.Take(inputs, result);

        return result;
    }


    TaskFileFormatRequirements Self(IRequirement requirement)
    {
        Requirements.Add(requirement);
        return this;
    }

    public TaskFileFormatRequirements RequiredOne(FileFormat format) => Required(format, 1);
    public TaskFileFormatRequirements Required(FileFormat format, uint amount) => Required(format, amount, amount);
    public TaskFileFormatRequirements RequiredAtLeast(FileFormat format, uint min) => Required(format, min, uint.MaxValue);
    public TaskFileFormatRequirements Required(FileFormat format, uint min, uint max) => Self(new MinMaxRequirement(format, min, max));

    public TaskFileFormatRequirements MaybeOne(FileFormat format) => Maybe(format, 1);
    public TaskFileFormatRequirements Maybe(FileFormat format, uint amount) => Maybe(format, 0, amount);
    TaskFileFormatRequirements Maybe(FileFormat format, uint min, uint max) => Self(new MinMaxRequirement(format, min, max));


    public IEnumerator<IRequirement> GetEnumerator() => Requirements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();



    public interface IRequirement
    {
        bool Required { get; }
        FileFormat Format { get; }

        void Take(List<FileWithFormat> files, List<FileWithFormat> addTo);
        OperationResult Check(ReceivedTask task);
    }
    record MinMaxRequirement(FileFormat Format, uint Minimum, uint Maximum) : IRequirement
    {
        bool IRequirement.Required => Minimum != 0;

        public void Take(List<FileWithFormat> files, List<FileWithFormat> addTo)
        {
            for (int i = 0; i < Maximum; i++)
            {
                var f = files.FirstOrDefault(x => x.Format == Format);
                if (f is null)
                {
                    if (i < Minimum) throw new Exception("Invalid amount of files");
                    break;
                }

                files.Remove(f);
                addTo.Add(f);
            }
        }
        public OperationResult Check(ReceivedTask task)
        {
            var count = task.InputFiles.Count(x => x.Format == Format);
            if (count < Minimum) return OperationResult.Err($"Not enough {Format} input files: {count}, should be at least {Minimum}");
            if (count > Maximum) return OperationResult.Err($"Too many {Format} input files: {count}, should be no more than {Maximum}");

            return true;
        }
    }
}
