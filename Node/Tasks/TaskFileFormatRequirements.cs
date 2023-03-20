using System.Collections;

namespace Node.Tasks;

public interface IHasRequirements<T> where T : IHasRequirements<T>
{
    T With(TaskFileFormatRequirements.IRequirement requirement);
}
public static class RequirementsExtensions
{
    public static T Either<T>(this IHasRequirements<T> req, Action<TaskFileFormatRequirements.EitherRequirement> func) where T : IHasRequirements<T> => req.With(new TaskFileFormatRequirements.EitherRequirement().With(func));

    public static T RequiredOne<T>(this IHasRequirements<T> req, FileFormat format) where T : IHasRequirements<T> => req.Required(format, 1);
    public static T Required<T>(this IHasRequirements<T> req, FileFormat format, uint amount) where T : IHasRequirements<T> => req.Required(format, amount, amount);
    public static T RequiredAtLeast<T>(this IHasRequirements<T> req, FileFormat format, uint min) where T : IHasRequirements<T> => req.Required(format, min, uint.MaxValue);
    public static T Required<T>(this IHasRequirements<T> req, FileFormat format, uint min, uint max) where T : IHasRequirements<T> => req.With(new TaskFileFormatRequirements.MinMaxRequirement(format, min, max));

    public static T MaybeOne<T>(this IHasRequirements<T> req, FileFormat format) where T : IHasRequirements<T> => req.Maybe(format, 1);
    public static T Maybe<T>(this IHasRequirements<T> req, FileFormat format, uint amount) where T : IHasRequirements<T> => req.Maybe(format, 0, amount);
    static T Maybe<T>(this IHasRequirements<T> req, FileFormat format, uint min, uint max) where T : IHasRequirements<T> => req.With(new TaskFileFormatRequirements.MinMaxRequirement(format, min, max));
}
public class TaskFileFormatRequirements : IEnumerable<TaskFileFormatRequirements.IRequirement>, IHasRequirements<TaskFileFormatRequirements>
{
    readonly List<IRequirement> Requirements = new();

    public TaskFileFormatRequirements() { }
    public TaskFileFormatRequirements(FileFormat singleRequired) => this.RequiredOne(singleRequired);

    public OperationResult Check(ReceivedTask task) => Requirements.Select(x => x.Check(task)).Merge();


    public TaskFileFormatRequirements With(IRequirement requirement)
    {
        Requirements.Add(requirement);
        return this;
    }


    public IEnumerator<IRequirement> GetEnumerator() => Requirements.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();



    public interface IRequirement
    {
        /// <value> List of all possible file formats in this requirement, but without repeats </value>
        IEnumerable<FileFormat> DistinctFormats { get; }
        bool Required { get; }

        OperationResult Check(ReceivedTask task);
    }
    public record MinMaxRequirement(FileFormat Format, uint Minimum, uint Maximum) : IRequirement
    {
        IEnumerable<FileFormat> IRequirement.DistinctFormats => new[] { Format };
        bool IRequirement.Required => Minimum != 0;

        public OperationResult Check(ReceivedTask task)
        {
            var count = task.InputFiles.Count(x => x.Format == Format);
            if (count < Minimum) return OperationResult.Err($"Not enough {Format} input files: {count}, should be at least {Minimum}");
            if (count > Maximum) return OperationResult.Err($"Too many {Format} input files: {count}, should be no more than {Maximum}");

            return true;
        }
    }

    public record EitherRequirement(ImmutableArray<IRequirement> Requirements, bool Required = false) : IRequirement, IHasRequirements<EitherRequirement>
    {
        IEnumerable<FileFormat> IRequirement.DistinctFormats => Requirements.SelectMany(r => r.DistinctFormats).Distinct();

        public EitherRequirement(bool required = false) : this(ImmutableArray<IRequirement>.Empty, required) { }

        EitherRequirement IHasRequirements<EitherRequirement>.With(IRequirement requirement) => OrThen(requirement);
        public EitherRequirement OrThen(IRequirement requirement) => this with { Requirements = Requirements.Add(requirement) };
        public EitherRequirement OrFail(IRequirement requirement) => this with { Required = true };

        OperationResult IRequirement.Check(ReceivedTask task)
        {
            var check = OperationResult.Err();
            foreach (var requirement in Requirements)
            {
                check = requirement.Check(task);
                if (check) return true;
            }

            return Required ? check : true;
        }
    }
}
