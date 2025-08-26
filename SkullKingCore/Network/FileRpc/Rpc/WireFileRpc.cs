using SkullKingCore.Network.Common;
using System.Runtime.Serialization;
using System.Xml;

namespace SkullKingCore.Network.FileRpc.Rpc
{
    /// <summary>
    /// FileRpc wire = DataContract + Binary XML, same settings/known types as the TCP wire.
    /// Different class name to avoid collisions with TCP Wire.
    /// </summary>
    internal static class WireFileRpc
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

        // passthrough hooks if you later add framing
        public static ReadOnlyMemory<byte> Wrap(ReadOnlyMemory<byte> payload) => payload;
        public static ReadOnlyMemory<byte> Unwrap(ReadOnlyMemory<byte> payload) => payload;
    }
}
