using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetsuiteSample
{
    public class NetsuiteOptions
    {
        public string AccountId { get; set; }

        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string TokenId { get; set; }
        public string TokenSecret { get; set; }

        public string Realm { get; set; }

        public string ScriptId { get; set; }
        public string DeploymentId { get; set; }
    }
}
