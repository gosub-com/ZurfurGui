using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZurfurGui.Base.Helpers;

internal struct Hasher
{
    static readonly ulong s_random = (ulong)new Random().NextInt64();


    ulong _hashValue;

    public Hasher()
    {
        _hashValue = s_random;
    }

    public Hasher(int firstValue)
    {
        _hashValue = s_random + (uint)firstValue;
    }

    public void Add(int value)
    {

        _hashValue = ((ulong)HashMix((uint)_hashValue) << 32)
            + (_hashValue >> 17) + (_hashValue >> 32) + (uint)value;
    }

    public override int GetHashCode()
    {
        return (int)HashMix((uint)((_hashValue >> 32) + _hashValue));
    }


    // Mix the hash to try and get better uniformity, hopefully
    // faster than mod with just as much random bit shuffling and mixing.
    // For all 4 billion numbers, it doesn't have any collisions.
    // This is based loosely on xoroshiro64* https://prng.di.unimi.it
    public static uint HashMix(uint i)
        => (Rol(i, 26) ^ i ^ Rol(i, 9)) * 0x9E3779BB;

    static uint Rol(uint i, int shift)
        => i << shift | i >> 32 - shift;
}
