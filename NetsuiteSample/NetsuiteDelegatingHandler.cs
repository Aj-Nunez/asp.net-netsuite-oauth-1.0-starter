using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NetsuiteSample
{
    public class NetsuiteDelegatingHandler : DelegatingHandler
    {
        private readonly OAuthService _oauth;
        private readonly NetsuiteOptions _netsuiteOptions;

        public NetsuiteDelegatingHandler(NetsuiteOptions netsuiteOptions, OAuthService oauth)
        {
            _oauth = oauth;
            _netsuiteOptions = netsuiteOptions;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var req = _oauth.SignRequest(request, _netsuiteOptions.ClientId, _netsuiteOptions.ClientSecret, _netsuiteOptions.TokenId, _netsuiteOptions.TokenSecret, _netsuiteOptions.Realm);

                return await base.SendAsync(req, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
                throw ex;
            }
        }
    }
}
