using System;
using JALib.Tools.ByteTool;

namespace JALib.Tools;

public class JARandom : Random {

    public static readonly JARandom Instance = new();

    public JARandom() {
    }

    public JARandom(int seed) : base(seed) {
    }

    public short NextShort() {
        return (short) Next();
    }

    public ushort NextUShort() {
        return (ushort) Next();
    }

    public int NextInt() {
        return Next();
    }

    public uint NextUInt() {
        return Next().ToBytes().ToUInt();
    }

    public long NextLong() {
        return NextBytes(8).ToLong();
    }

    public ulong NextULong() {
        return NextBytes(8).ToULong();
    }

    public float NextFloat() {
        return (float) NextDouble();
    }

    public decimal NextDecimal() {
        return new decimal([Next(), Next(), Next(), Next()]);
    }

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