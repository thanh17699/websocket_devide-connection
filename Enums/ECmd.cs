using System.ComponentModel;

namespace Vidoc.Socket.Enums
{
    public enum ECmd
    {
        [Description("register")]
        reg = 1,
        [Description("sendlog")]
        sendlog = 2,
    }
}
