using System;
using System.Threading;

namespace RabbitMQToHTTPLoadBalancingService
{
    public class BaseRequest
    {
        public string Id { get; set; }
        public object Params { get; set; }
        public string Jsonrpc { get; set; }
        public string Method { get; set; }

    }
}
