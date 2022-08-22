namespace Common.Tasks.Model;

public abstract record TaskRequirementBase(string Name, ImmutableArray<string>? Versions);

public record TaskSoftwareRequirement(string Name, ImmutableArray<string>? Versions, ImmutableArray<TaskPluginRequirement>? Plugins) : TaskRequirementBase(Name, Versions);
public record TaskPluginRequirement(string Name, ImmutableArray<string>? Versions, ImmutableArray<TaskSubpluginRequirement>? Subplugins) : TaskRequirementBase(Name, Versions);
public record TaskSubpluginRequirement(string Name, ImmutableArray<string>? Versions) : TaskRequirementBase(Name, Versions);