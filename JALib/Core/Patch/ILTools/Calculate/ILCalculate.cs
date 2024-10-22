namespace JALib.Core.Patch.ILTools.Calculate;

public abstract class ILCalculate : ILCode {
    public ILCode Left;
    public ILCode Right;

    public ILCalculate(ILCode left, ILCode right) {
        if(left.ReturnType != right.ReturnType) throw new InvalidProgramException("Type mismatch");
        Left = left;
        Right = right;
    }

    public override Type ReturnType => Left.ReturnType;
}