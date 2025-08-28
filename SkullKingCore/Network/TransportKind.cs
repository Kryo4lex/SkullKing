using System.ComponentModel;

namespace SkullKingCore.Network
{
    public enum TransportKind : int
    {
        [Description("TCP (sockets)")] Tcp = 1,
        [Description("FileRpc (shared folder)")] FileRpc = 2,
        [Description("Web RPC — built-in web server (Kestrel), HTTP long-poll, single port")] WebRpc = 3,
    }
}
