namespace Vidoc.Socket.Configs
{
    public class AppConfig
    {
        public SocketServer SocketServer { get; set; }
        public VidocUri VidocUri { get; set; }
        public string XApiKey { get; set; }
    }

    public class SocketServer
    {
        public string Localaddr { get; set; }
        public int Port { get; set; }
    }
    public class VidocUri
    {
        public string Checkin { get; set; }
    }
}
