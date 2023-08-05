using System.Numerics;
using System.Runtime.CompilerServices;

namespace UpdaterCommon;

public static class XXHash
{
    const ulong Prime1 = 11400714785074694791;
    const ulong Prime2 = 14029467366897019727;
    const ulong Prime3 = 1609587929392839161;
    const ulong Prime4 = 9650029242287828579;
    const ulong Prime5 = 2870177450012600261;


    public static ulong XXH64(string file)
    {
        if (new FileInfo(file).Length < 1024 * 1024)
            return XXH64(File.ReadAllBytes(file));

        using var stream = File.OpenRead(file);
        return XXH64(stream);
    }
    public static ulong XXH64(ReadOnlySpan<byte> input)
    {
        var state = new State(0);
        state.TotalLength = (ulong)input.Length;
        UpdateState(ref state, input);

        return DigestState(ref state);
    }
    public static ulong XXH64(Stream stream) => XXH64(stream, out _);
    public static ulong XXH64(Stream stream, out long size)
    {
        var state = new State(0);
        var buffer = new byte[1024 * 1024];
        size = 0;

        var pos = 0;

        while (true)
        {
            var read = stream.Read(buffer.AsSpan(pos));
            if (read == 0) break;
            size += read;
            pos += read;

            state.TotalLength += (ulong)pos;
            UpdateState(ref state, buffer.AsSpan(0, pos));

            state.TempMemory.CopyTo(buffer);
            pos %= 32;
            state.TotalLength -= (ulong)pos;
            if (pos == 0) state.TempMemory = default;
        }

        state.TotalLength += (ulong)state.TempMemory.Length;
        return DigestState(ref state);
    }


    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    static void UpdateState(ref State state, ReadOnlySpan<byte> input)
    {
        if (input.Length >= 32)
        {
            var v1 = state.A1;
            var v2 = state.A2;
            var v3 = state.A3;
            var v4 = state.A4;

            do
            {
                v1 = Round(v1, BitConverter.ToUInt64(input.Slice(0 * sizeof(ulong))));
                v2 = Round(v2, BitConverter.ToUInt64(input.Slice(1 * sizeof(ulong))));
                v3 = Round(v3, BitConverter.ToUInt64(input.Slice(2 * sizeof(ulong))));
                v4 = Round(v4, BitConverter.ToUInt64(input.Slice(3 * sizeof(ulong))));

                input = input.Slice(4 * sizeof(ulong));
            }
            while (input.Length >= 32);

            state.A1 = v1;
            state.A2 = v2;
            state.A3 = v3;
            state.A4 = v4;
        }

        state.TempMemory = input;
    }
    static ulong DigestState(ref State state)
    {
        ulong hash;

        if (state.TotalLength >= 32)
        {
            var v1 = state.A1;
            var v2 = state.A2;
            var v3 = state.A3;
            var v4 = state.A4;

            hash = BitOperations.RotateLeft(v1, 1) + BitOperations.RotateLeft(v2, 7) + BitOperations.RotateLeft(v3, 12) + BitOperations.RotateLeft(v4, 18);
            hash = MergeRound(hash, v1);
            hash = MergeRound(hash, v2);
            hash = MergeRound(hash, v3);
            hash = MergeRound(hash, v4);
        }
        else hash = state.A3 + Prime5;

        hash += state.TotalLength;

        while (state.TempMemory.Length >= sizeof(ulong))
        {
            hash ^= Round(0, ReadULong(ref state.TempMemory));
            hash = BitOperations.RotateLeft(hash, 27) * Prime1 + Prime4;
        }

        if (state.TempMemory.Length >= sizeof(uint))
        {
            hash ^= ReadUInt(ref state.TempMemory) * Prime1;
            hash = BitOperations.RotateLeft(hash, 23) * Prime2 + Prime3;
        }

        for (int i = 0; i < state.TempMemory.Length; i++)
        {
            hash ^= state.TempMemory[i] * Prime5;
            hash = BitOperations.RotateLeft(hash, 11) * Prime1;
        }

        hash ^= hash >> 33;
        hash *= Prime2;
        hash ^= hash >> 29;
        hash *= Prime3;
        hash ^= hash >> 32;

        return hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static ulong Round(ulong acc, ulong input)
    {
        acc += input * Prime2;
        acc = BitOperations.RotateLeft(acc, 31);
        acc *= Prime1;

        return acc;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    static ulong MergeRound(ulong acc, ulong val)
    {
        val = Round(0, val);
        acc ^= val;
        acc = acc * Prime1 + Prime4;

        return acc;
    }


    [MethodImpl(256)]
    static ulong ReadULong(ref ReadOnlySpan<byte> bytes)
    {
        var value = BitConverter.ToUInt64(bytes);
        bytes = bytes.Slice(sizeof(ulong));
        return value;
    }
    [MethodImpl(256)]
    static uint ReadUInt(ref ReadOnlySpan<byte> bytes)
    {
        var value = BitConverter.ToUInt32(bytes);
        bytes = bytes.Slice(sizeof(uint));
        return value;
    }


    ref struct State
    {
        public ulong TotalLength, Seed, A1, A2, A3, A4;
        public ReadOnlySpan<byte> TempMemory;

        public State(ulong seed)
        {
            Seed = seed;
            A1 = seed + Prime1 + Prime2;
            A2 = seed + Prime2;
            A3 = seed + 0;
            A4 = seed - Prime1;

            TempMemory = default;
            TotalLength = default;
        }
    }
}