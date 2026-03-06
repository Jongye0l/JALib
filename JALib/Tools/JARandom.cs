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

    public uint NextUInt() => (uint) Next();

    public long NextLong() => (long) Next() << 32 | (uint) Next();

    public ulong NextULong() => (ulong) NextLong();

    public float NextFloat() => (float) NextDouble();

    public float NextAllFloat() => Next().AsUnsafe<int, float>();

    public unsafe double NextAllDouble() {
        long value = NextLong();
        return *(double*) &value;
    }

    public decimal NextDecimal() => new([Next(), Next(), Next(), Next()]);

    public override unsafe void NextBytes(byte[] buffer) {
        int i = 0;
        fixed(byte* ptr = buffer) {
            while(i + 4 <= buffer.Length) {
                int* i1 = (int*) ptr[i];
                *i1 = Next();
                i += 4;
            }
            if(i >= buffer.Length) return;
            int temp = Next();
            byte* b1 = (byte*) &temp;
            while(i < buffer.Length) {
                ptr[i] = *b1;
                b1++;
                i++;
            }
        }
    }

    public byte[] NextBytes(int count) {
        byte[] buffer = new byte[count];
        NextBytes(buffer);
        return buffer;
    }
}