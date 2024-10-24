using System.Reflection.Emit;

namespace JALib.Core.Patch.ILTools;

public class ILLocal : ILTool {
    public LocalBuilder LocalBuilder;
    public Type type;

    public ILLocal(LocalBuilder local) {
        LocalBuilder = local;
        type = local.LocalType;
    }

    public ILLocal(Type type) {
        LocalBuilder = null;
        this.type = type;
    }

    public int Index { get; internal set; }

    public void Setup(ILGenerator generator) {
        LocalBuilder ??= generator.DeclareLocal(type);
    }
}