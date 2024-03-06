using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetsuiteSample
{
    public class NetsuiteService : INetsuiteService
    {
        private readonly IHttpClientFactory _http;
        private readonly NetsuiteOptions _options;

        public NetsuiteService(IHttpClientFactory http, NetsuiteOptions options)
        {
            _http = http;
            _options = options;
        }

        public async Task<TResponse> GetAsync<TResponse>(IEnumerable<KeyValuePair<string, string>> args = null)
        {
            var client = _http.CreateClient("netsuite");
            var url = "https://3469472-sb1.suitetalk.api.netsuite.com/services/rest/record/v1/customer/227";
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get
            };

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(content);
        }

        public async Task<TResponse> PostAsync<TResponse, TRequest>(TRequest body)
        {
            var client = _http.CreateClient("netsuite");

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("https://" + _options.AccountId + ".restlets.api.netsuite.com/app/site/hosting/restlet.nl?script=" + _options.ScriptId + "&deploy=" + _options.DeploymentId),
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(body))
            };

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TResponse>(content);
        }
    }
}
