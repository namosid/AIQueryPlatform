using AIQueryPlatform.LLMServiceOperator.Models;
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
        Task<string> AskAsync(SearchOutput entityOutput, string prompt, MemoryTurn turn);
    }
}
