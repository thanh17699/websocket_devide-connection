namespace idoc.Socket.TerminalSendMessage
{
    public class TsSendlogMessage
    {
        public string cmd { get; set; }
        public string sn { get; set; }
        public int count { get; set; }
        public int logindex { get; set; }
        public List<Record> record { get; set; }
    }

    public class Record
    {
        public int enrollid { get; set; }
        public string time { get; set; }
        public int mode { get; set; }
        public int inout { get; set; }
        public int @event { get; set; }
        public double temp { get; set; }
        public int verifymode { get; set; }
        public string image { get; set; }
    }
}
