using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiGateway.Core.Abstractions
{
    internal interface IRateLimitStore
    {
        //analisar melhor
        Task<long> IncrementAsync(string key, TimeSpan window);
    }
}
