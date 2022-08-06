using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetsuiteSample
{
    public interface INetsuiteService
    {
        public Task<TResponse> GetAsync<TResponse>(IEnumerable<KeyValuePair<string, string>> args = null);

        public Task<TResponse> PostAsync<TResponse, TRequest>(TRequest body);
    }
}
