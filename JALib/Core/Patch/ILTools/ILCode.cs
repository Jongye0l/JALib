using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace JALib.Core.Patch.ILTools;

public abstract class ILCode : ILTool {
    public List<ExceptionBlock> Blocks = [];
    public List<Label> Labels = [];

    public abstract Type ReturnType { get; }

    public abstract IEnumerable<CodeInstruction> Load(ILGenerator generator);

    public abstract override string ToString();
}