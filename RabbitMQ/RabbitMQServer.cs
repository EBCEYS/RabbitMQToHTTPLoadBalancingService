using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQToHTTPLoadBalancingService.Configuration;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RabbitMQToHTTPLoadBalancingService.RabbitMQ
{
    public class RabbitMQServer
    {
        private readonly RabbitMQConfig config;
        private readonly Logger logger;

        private readonly IConnection connection;
        private readonly IModel channel;
        public readonly EventingBasicConsumer consumer;

        public RabbitMQServer(RabbitMQConfig config, Logger logger)
        {
            this.config = config;
            this.logger = logger;
            ConnectionFactory factory = new()
            {
                HostName = this.config.HostName,
                UserName = this.config.UserName,
                Password = this.config.Password,
                Port = this.config.Port
            };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.QueueDeclare(queue: this.config.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            channel.BasicQos(0, 1, false);
            consumer = new(channel);
            channel.BasicConsume(queue: this.config.QueueName, autoAck: false, consumer: consumer);

            consumer.Received += (model, ea) =>
            {
                string response = null;

                byte[] body = ea.Body.ToArray();
                IBasicProperties props = ea.BasicProperties;
                IBasicProperties replyProps = channel.CreateBasicProperties();
                replyProps.CorrelationId = props.CorrelationId;
                BaseResponse baseResponse = new();
                BaseRequest baseRequest;
                try
                {
                    string message = Encoding.UTF8.GetString(body);
                    baseRequest = IPStorage.ToObject<BaseRequest>(message);
                    baseResponse.Id = baseRequest.Id;
                    baseResponse.Result = new()
                    {
                        Answer = Result.ERROR
                    };
                    this.logger.Info("Get message {message} from queue {queue}}", message, this.config.QueueName);
                    response = IPStorage.RequestAndResponse(baseRequest, config.Timeout);
                    if (!string.IsNullOrEmpty(response))
                    {
                        baseResponse = IPStorage.ToObject<BaseResponse>(response);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error on rabbit request! {exmessage}", ex.Message);
                }
                finally
                {
                    string resp = IPStorage.ToJson(baseResponse);
                    this.logger.Info("On request {id} response is {resp}", baseResponse.Id, resp);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(resp);
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };
        }
    }
}
