using NLog;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQToHTTPLoadBalancingService.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

                try
                {
                    string message = Encoding.UTF8.GetString(body);
                    this.logger.Info("Get message {message} from queue {queue}}", message, this.config.QueueName);
                    response = IPStorage.RequestAndResponse(message, config.Timeout).Result;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error on getting rabbit request! {exmessage}", ex.Message);
                    response = "";
                }
                finally
                {
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    channel.BasicPublish(exchange: "", routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
            };
        }
    }
}
