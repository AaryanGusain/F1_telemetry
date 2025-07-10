using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace F1Parser
{
    /// <summary>
    /// Minimal helper for sequentially reading little-endian primitive types from a byte span.
    /// Keeps an internal offset so callers can advance naturally.
    /// </summary>
    public ref struct LittleEndianReader
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _offset;

        public LittleEndianReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            _offset = 0;
        }

        public int Offset => _offset;
        public int Remaining => _data.Length - _offset;

        public void Skip(int count)
        {
            _offset += count;
        }

        public byte ReadUInt8() => Read<byte>();
        public sbyte ReadInt8() => unchecked((sbyte)Read<byte>());
        public ushort ReadUInt16() => Read<ushort>();
        public short ReadInt16() => Read<short>();
        public uint ReadUInt32() => Read<uint>();
        public int ReadInt32() => Read<int>();
        public ulong ReadUInt64() => Read<ulong>();
        public long ReadInt64() => Read<long>();
        public float ReadFloat()
        {
            uint val = ReadUInt32();
            return BitConverter.Int32BitsToSingle((int)val);
        }

        private T Read<T>() where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            if (_offset + size > _data.Length)
                throw new ArgumentOutOfRangeException(nameof(size), "Attempt to read beyond end of span.");

            ReadOnlySpan<byte> slice = _data.Slice(_offset, size);
            _offset += size;

            if (typeof(T) == typeof(byte))
                return (T)(object)slice[0];
            if (typeof(T) == typeof(ushort))
                return (T)(object)BinaryPrimitives.ReadUInt16LittleEndian(slice);
            if (typeof(T) == typeof(short))
                return (T)(object)BinaryPrimitives.ReadInt16LittleEndian(slice);
            if (typeof(T) == typeof(uint))
                return (T)(object)BinaryPrimitives.ReadUInt32LittleEndian(slice);
            if (typeof(T) == typeof(int))
                return (T)(object)BinaryPrimitives.ReadInt32LittleEndian(slice);
            if (typeof(T) == typeof(ulong))
                return (T)(object)BinaryPrimitives.ReadUInt64LittleEndian(slice);
            if (typeof(T) == typeof(long))
                return (T)(object)BinaryPrimitives.ReadInt64LittleEndian(slice);

            throw new NotSupportedException($"Type {typeof(T).Name} not supported in LittleEndianReader.Read<T>.");
        }

        public string ReadFixedString(int length)
        {
            if (_offset + length > _data.Length)
                throw new ArgumentOutOfRangeException(nameof(length));
            var slice = _data.Slice(_offset, length);
            _offset += length;
            int nullIdx = slice.IndexOf((byte)0);
            if (nullIdx >= 0)
                slice = slice.Slice(0, nullIdx);
            return System.Text.Encoding.UTF8.GetString(slice);
        }
    }
} 