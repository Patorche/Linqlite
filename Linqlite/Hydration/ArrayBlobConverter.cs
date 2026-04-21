using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Linqlite.Hydration
{
    public static class ArrayBlobConverter<T>
    {
        public static byte[] ToBytes(T[] values)
        {
            if (values == null)
                return null;

            int sizeT = Unsafe.SizeOf<T>();
            int size = values.Length * sizeT;
            byte[] result = new byte[size];
            Buffer.BlockCopy(values, 0, result, 0, size);
            return result;
        }

        public static T[] FromBytes(byte[] bytes)
        {
            if (bytes == null)
                return null;
            
            int sizeT = Unsafe.SizeOf<T>();
            int count = bytes.Length / sizeT;
            T[] result = new T[count];
            unsafe
            {
                fixed (byte* src = bytes)
                fixed (T* dst = result)
                {
                    Buffer.MemoryCopy(src, dst, bytes.Length, bytes.Length);
                }
            }
            return result;
        }

       
    }

    public static class ArrayBlobConverter
    {
        public static byte[] ToBytes(Type elementType, Array array)
        {
            var converterType = typeof(ArrayBlobConverter<>).MakeGenericType(elementType);
            var method = converterType.GetMethod("ToBytes", BindingFlags.Public | BindingFlags.Static);
            return (byte[])method.Invoke(null, new object[] { array });
        }

        public static Array FromBytes(Type elementType, byte[] blob)
        {
            var converterType = typeof(ArrayBlobConverter<>).MakeGenericType(elementType);
            var method = converterType.GetMethod("FromBytes", BindingFlags.Public | BindingFlags.Static);
            return (Array)method.Invoke(null, new object[] { blob });
        }
    }

}
