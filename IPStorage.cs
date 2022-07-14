using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQToHTTPLoadBalancingService
{
    public static class IPStorage
    {
        /// <summary>
        /// Key - ip,
        /// Value - current load
        /// </summary>
        public static ConcurrentDictionary<string, int> IpsDictionary { get; set; } = new();
        private static Logger logger;

        public static void SetIps(List<string> ips, Logger logger)
        {
            IPStorage.logger = logger;
            IpsDictionary = new();
            foreach (string ip in ips)
            {
                if (!IpsDictionary.TryGetValue(ip, out _))
                {
                    IpsDictionary[ip] = 0;
                }
            }
            IPStorage.logger.Info("Set ips: {@IpsDictionary}", IpsDictionary);
        }

        public static async Task<string> RequestAndResponse(string message, int timeout)
        {
            HttpClientHandler handler = new();
            HttpClient httpClient = new(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };

            string ip = GetLeastBusyIp() ?? throw new Exception("Can not find any ips!");
            IpsDictionary[ip]++;
            UriBuilder uriBuilder = new(ip);
            string id = Guid.NewGuid().ToString();
            logger.Info("Try to post request {id} to {ip} with data {message}", id, uriBuilder.Uri.AbsoluteUri, message);
            HttpRequestMessage request = new(HttpMethod.Post, uriBuilder.Uri)
            {
                Content = new StringContent(message, UTF8Encoding.UTF8)
            };
            string responseString = null;
            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(request);
                responseString = await response.Content.ReadAsStringAsync();
                logger.Info("Get response {id}, {response}", id, responseString);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error on sending http request! {ex}", ex.Message);
            }
            IpsDictionary[ip]--;
            return responseString;
        }

        private static string GetLeastBusyIp()
        {
            if (IpsDictionary.Any())
            {
                KeyValuePair<string, int> result = IpsDictionary.FirstOrDefault();
                foreach (KeyValuePair<string, int> val in IpsDictionary.ToArray())
                {
                    if (val.Value <= result.Value)
                    {
                        result = val;
                    }
                }
                return result.Key;
            }
            return null;
        }
    }
}
