using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NetsuiteSample
{
    public class OAuthService
    {

        public OAuthService()
        {
        }

        public HttpRequestMessage SignRequest(HttpRequestMessage message, string consumerKey, string consumerSecret, string tokenKey, string tokenSecret, string realm)
        {
            string header = OAuthBase.GenerateAuthorizationHeaderValue(message.RequestUri, consumerKey, consumerSecret, tokenKey, tokenSecret, message.Method.Method, realm);

            return new HttpRequestMessage
            {
                RequestUri = message.RequestUri,
                Method = message.Method,
                Headers =
                {
                    { "Authorization", header }
                },
                Content = message.Content
            };
        }
    }
}
