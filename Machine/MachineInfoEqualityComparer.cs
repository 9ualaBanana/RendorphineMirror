using System.Diagnostics.CodeAnalysis;

namespace Machine;

public class MachineInfoDTOEqualityComparer : IEqualityComparer<MachineInfo.DTO>
{
    public bool Equals(MachineInfo.DTO? x, MachineInfo.DTO? y)
    {
        return x?.PCName == y?.PCName;
    }

    public int GetHashCode([DisallowNull] MachineInfo.DTO obj)
    {
        return obj.GetHashCode();
    }
}
