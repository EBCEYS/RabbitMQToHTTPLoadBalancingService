using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = false,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

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

        public static string ToJson<T>(T obj)
        {
            return JsonSerializer.Serialize(obj, jsonSerializerOptions);
        }

        public static T ToObject<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, jsonSerializerOptions);
        }

        public static string RequestAndResponse(BaseRequest message, int timeout)
        {
            List<string> listOfExcludedIps = new();
            string responseString = null;
            HttpClientHandler handler = new();
            HttpClient httpClient = new(handler)
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            for (int i = 0; i < IpsDictionary.Count; i++)
            {
                string ip = GetLeastBusyIp(listOfExcludedIps);
                if (string.IsNullOrEmpty(ip))
                {
                    break;
                }
                IpsDictionary[ip]++;
                UriBuilder uriBuilder = new($"{ip}/{message.Method ?? throw new Exception("Method is null!")}");
                string id = Guid.NewGuid().ToString();
                logger.Info("Try to post request {id} to {ip} with data {@message}", id, uriBuilder.Uri.AbsoluteUri, message);
                HttpRequestMessage request = new(HttpMethod.Post, uriBuilder.Uri)
                {
                    Content = new StringContent(ToJson(message), UTF8Encoding.UTF8)
                };
                bool result = SendRequest(httpClient, request, id, out responseString);
                if (!result)
                {
                    IpsDictionary[ip]--;
                    listOfExcludedIps.Add(ip);
                }
            }
            
            return responseString;
        }

        private static bool SendRequest(HttpClient client, HttpRequestMessage message, string id, out string responseString)
        {
            try
            {
                HttpResponseMessage response = client.SendAsync(message).Result;
                responseString = response.Content.ReadAsStringAsync().Result;
                logger.Info("Get response {id}, {response}", id, responseString);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error on sending http request! {ex}", ex.Message);
            }
            responseString = null;
            return false;
        }

        private static string GetLeastBusyIp(List<string> excludedIps = null)
        {
            if (IpsDictionary.Any())
            {
                KeyValuePair<string, int> result = IpsDictionary.FirstOrDefault();
                bool smthgChanged = false;
                foreach (KeyValuePair<string, int> val in IpsDictionary.ToArray())
                {
                    if (val.Value <= result.Value && !(excludedIps?.Exists(x => x == val.Key) ?? false))
                    {
                        result = val;
                        smthgChanged = true;
                    }
                }
                return smthgChanged ? result.Key : null;
            }
            return null;
        }
    }
}
