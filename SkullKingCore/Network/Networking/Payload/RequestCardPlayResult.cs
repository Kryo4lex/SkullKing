using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkullKingCore.Network.Networking.Payload
{
    public sealed class RequestCardPlayResult
    {
        public int Index { get; set; }          // which card in Hand was chosen
        public string? TigressMode { get; set; } // optional: "Escape" or "Pirate"
    }
}
