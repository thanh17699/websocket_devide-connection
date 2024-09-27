namespace idoc.Socket.TerminalMessage
{
    public class TsRegisterMessage
    {
        public string cmd { get; set; }
        public string sn { get; set; }
        public int cpusn { get; set; }
        public Devinfo devinfo { get; set; }
    }

    public class Devinfo
    {
        public string modelname { get; set; }
        public int usersize { get; set; }
        public int fpsize { get; set; }
        public int cardsize { get; set; }
        public int pwdsize { get; set; }
        public int logsize { get; set; }
        public int useduser { get; set; }
        public int usedfp { get; set; }
        public int usedcard { get; set; }
        public int usedpwd { get; set; }
        public int usedlog { get; set; }
        public int usednewlog { get; set; }
        public string fpalgo { get; set; }
        public string firmware { get; set; }
        public string time { get; set; }
        public string mac { get; set; }
    }
}
