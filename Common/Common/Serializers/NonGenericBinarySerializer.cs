using System;
using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace ServiceBlocks.Common.Serializers
{
    public static class NonGenericBinarySerializer
    {
        public static byte[] SerializeToByteArray(object item)
        {
            var bin = new BinaryFormatter();
            using (var memStream = new MemoryStream())
            {
                bin.AssemblyFormat = FormatterAssemblyStyle.Simple;
                bin.Serialize(memStream, item);
                memStream.Close();
                return memStream.ToArray();
            }
        }

        public static string SerializeToBase64(object item)
        {
            return Convert.ToBase64String(SerializeToByteArray(item));
        }

        public static object DeSerializeFromByteArray(byte[] dataArray)
        {
            using (var memStream = new MemoryStream(dataArray))
            {
                var bin = new BinaryFormatter();
                bin.AssemblyFormat = FormatterAssemblyStyle.Simple;
                object resultObject = bin.Deserialize(memStream);
                memStream.Close();
                return resultObject;
            }
        }

        public static object DeSerializeFromBase64(string data)
        {
            return DeSerializeFromByteArray(Convert.FromBase64String(data));
        }
    }
}