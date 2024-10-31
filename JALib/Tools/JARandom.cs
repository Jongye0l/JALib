using JALib.Tools.ByteTool;

namespace JALib.Tools;

public class JARandom : Random {

    public static readonly JARandom Instance = new();

    public JARandom() {
    }

    public JARandom(int seed) : base(seed) {
    }

    public short NextShort() => (short) Next();

    public ushort NextUShort() => (ushort) Next();

    public int NextInt() => Next();

    public uint NextUInt() => Next().ToBytes().ToUInt();

    public long NextLong() => NextBytes(8).ToLong();

    public ulong NextULong() => NextBytes(8).ToULong();

    public float NextFloat() => (float) NextDouble();

    public float NextAllFloat() => Next().ToBytes().ToFloat();

    public double NextAllDouble() => NextBytes(8).ToDouble();

    public decimal NextDecimal() => new([Next(), Next(), Next(), Next()]);

    public override void NextBytes(byte[] buffer) {
        int i = 0;
        while(i + 4 <= buffer.Length) {
            Next().ToBytes(buffer, i);
            i += 4;
        }
        if(i < buffer.Length) Array.Copy(Next().ToBytes(), 0, buffer, i, buffer.Length - i);
    }

    public byte[] NextBytes(int count) {
        byte[] buffer = new byte[count];
        NextBytes(buffer);
        return buffer;
    }
}