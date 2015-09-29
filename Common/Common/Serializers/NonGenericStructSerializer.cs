using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ServiceBlocks.Common.Serializers
{
    public static class NonGenericStructSerializer
    {
        public static object Deserialize(byte[] data, Type t)
        {
            int objsize = Marshal.SizeOf(t);

            IntPtr ptr = Marshal.AllocHGlobal(objsize);

            try
            {
                Marshal.Copy(data, 0, ptr, objsize);

                object retStruct = Marshal.PtrToStructure(ptr, t);

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

        public static List<object> DeserializeList(byte[] data, Type t)
        {
            var ptr = new IntPtr();

            int size;

            try
            {
                size = Marshal.SizeOf(t);

                ptr = Marshal.AllocHGlobal(size);

                int chunks = data.Length/size;

                var messages = new List<object>(chunks);

                object message;

                for (int i = 0; i < chunks; i++)
                {
                    Marshal.Copy(data, i*size, ptr, size);

                    message = Marshal.PtrToStructure(ptr, t);

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

        public static byte[] Serialize(object item, Type t)
        {
            int size = Marshal.SizeOf(t);

            IntPtr ptr = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(item, ptr, true);

                var temp = new byte[size];

                Marshal.Copy(ptr, temp, 0, size);

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

        public static byte[] SerializeList(List<object> objects, Type t)
        {
            int size = Marshal.SizeOf(t);

            int count = objects.Count;

            var array = new byte[count*size];

            for (int i = 0; i < count; i++)
            {
                Buffer.BlockCopy(Serialize(objects[i], t)
                    , 0, array, size*i, size);
            }

            return array;
        }
    }
}