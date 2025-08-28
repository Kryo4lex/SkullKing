using SkullKingCore.Network.Common;
using System.Runtime.Serialization;
using System.Xml;

namespace SkullKingCore.Network.WebRpc.Rpc
{
    internal static class WireWebRpc
    {
        private static DataContractSerializer GetSerializer(Type t)
        {
            var settings = new DataContractSerializerSettings
            {
                KnownTypes = KnownTypes.All,
                MaxItemsInObjectGraph = int.MaxValue,
                IgnoreExtensionDataObject = false,
                PreserveObjectReferences = false,
            };
            return new DataContractSerializer(t, settings);
        }

        public static byte[] Serialize<T>(T value)
        {
            using var ms = new MemoryStream();
            using var xw = XmlDictionaryWriter.CreateBinaryWriter(ms);
            GetSerializer(typeof(T)).WriteObject(xw, value!);
            xw.Flush();
            return ms.ToArray();
        }

        public static T Deserialize<T>(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var xr = XmlDictionaryReader.CreateBinaryReader(ms, XmlDictionaryReaderQuotas.Max);
            return (T)GetSerializer(typeof(T)).ReadObject(xr)!;
        }
    }
}
