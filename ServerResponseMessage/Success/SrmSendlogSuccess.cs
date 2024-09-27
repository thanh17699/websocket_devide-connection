namespace Vidoc.Socket.ServerResponseMessage.Success
{
    public class SrmSendlogSuccess
    {
        public string ret { get; set; }
        public bool result { get; set; }
        public int count { get; set; }
        public int logindex { get; set; }
        public string cloudtime { get; set; }
        public int access { get; set; }
    }
}
