using Phoenix.Rendering;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Phoenix.AssetImport
{
    public static class BinaryReaderTools
    {
        public static T ReadStruct<T>(this BinaryReader br)
            where T : unmanaged
        {
            T value = default;
            var span = MemoryMarshal.AsBytes(
                MemoryMarshal.CreateSpan(ref value, 1));
            br.Read(span);
            return value;
        }

        public static T[] ReadArray<T>(this BinaryReader br, int count)
            where T : unmanaged
        {
            var array = new T[count];
            var span = MemoryMarshal.AsBytes(array.AsSpan());

            int bytesToRead = span.Length;
            int bytesRead = 0;

            while (bytesRead < bytesToRead)
            {
                int read = br.Read(span.Slice(bytesRead));
                if (read == 0)
                    throw new EndOfStreamException();

                bytesRead += read;
            }

            return array;
        }
    }
}
