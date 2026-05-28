using AIQueryPlatform.LLMServiceOperator.Models.STM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Interface
{
    public interface ILlmService
    {
        Task<string> AskAsync(string schemaJson, string prompt, MemoryTurn turn);
    }
}
