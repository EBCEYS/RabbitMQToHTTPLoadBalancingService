namespace RabbitMQToHTTPLoadBalancingService
{
    public class BaseResponse
    {
        public string Id { get; set; }
        public ActionResponse Result { get; set; }
        public string Jsonrpc { get; set; }
        public BaseResponse()
        {
            Jsonrpc = "2.0";
        }
    }
}
