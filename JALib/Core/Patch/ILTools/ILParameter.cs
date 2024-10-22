namespace JALib.Core.Patch.ILTools;

public class ILParameter(int index, Type type, string name) : ILTool {
    public readonly int Index = index;
    public readonly string Name = name;
    public readonly Type Type = type;
}