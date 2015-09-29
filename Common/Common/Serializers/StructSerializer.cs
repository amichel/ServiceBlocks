using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ServiceBlocks.Common.Serializers
{
    public static class StructSerializer<T> where T : struct
    {
        public static T Deserialize(byte[] data)
        {
            int objsize = Marshal.SizeOf(typeof (T));

            IntPtr ptr = Marshal.AllocHGlobal(objsize);

            try
            {
                Marshal.Copy(data, 0, ptr, objsize);

                var retStruct = (T) Marshal.PtrToStructure(ptr, typeof (T));

                return retStruct;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);

                    ptr = IntPtr.Zero;
                }
            }
        }


        public static byte[] SerializeList(IList<T> structs)
        {
            int structSize = Marshal.SizeOf(typeof (T));

            int structsCount = structs.Count;

            var structsArray = new byte[structsCount*structSize];

            for (int i = 0; i < structsCount; i++)
            {
                Buffer.BlockCopy(Serialize(structs[i])
                    , 0, structsArray, structSize*i, structSize);
            }

            return structsArray;
        }

        public static byte[] Serialize(T item)
        {
            int structSize = Marshal.SizeOf(typeof (T));

            IntPtr ptr = Marshal.AllocHGlobal(structSize);

            try
            {
                Marshal.StructureToPtr(item, ptr, true);

                var temp = new byte[structSize];

                Marshal.Copy(ptr, temp, 0, structSize);

                return temp;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);

                    ptr = IntPtr.Zero;
                }
            }
        }

        public static List<T> DeserializeList(byte[] data)
        {
            var ptr = new IntPtr();

            int size;

            try
            {
                Type typeOfT = typeof (T);

                size = Marshal.SizeOf(typeOfT);

                ptr = Marshal.AllocHGlobal(size);

                int chunks = data.Length/size;

                var messages = new List<T>(chunks);

                var message = new T();

                for (int i = 0; i < chunks; i++)
                {
                    Marshal.Copy(data, i*size, ptr, size);

                    message = (T) Marshal.PtrToStructure(ptr, typeOfT);

                    messages.Add(message);
                }

                return messages;
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptr);
                    ptr = IntPtr.Zero;
                }
            }
        }
    }
}