namespace Vidoc.Socket.ServerResponseMessage.Http
{
    public class HttpResponseData
    {
        public int statusCode { get; set; }
        public string? message { get; set; }
        public object? data { get; set; }
    }
}
