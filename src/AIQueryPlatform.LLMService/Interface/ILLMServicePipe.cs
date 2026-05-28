using AIQueryPlatform.LLMServiceOperator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Interface
{
    public interface ILLMServicePipe
    {
        Task<LLMResponse> ProcessQuery(string query, TenantData tenant);
    }
}
