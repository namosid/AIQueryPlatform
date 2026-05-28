using Azure;
using Azure.AI.OpenAI;
using OpenAI;
using AIQueryPlatform.LLMServiceOperator.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly AzureOpenAIClient _client;
        private readonly string _deploymentName;
        public EmbeddingService(string apiKey, string endPoint, string deploymentName)
        {
            _client = new AzureOpenAIClient(new Uri(endPoint), new AzureKeyCredential(apiKey));
            _deploymentName = deploymentName;
        }

        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            var embeddingClient = _client.GetEmbeddingClient(_deploymentName);

            var result = await embeddingClient.GenerateEmbeddingAsync(text);

            return result.Value.ToFloats().ToArray();
        }
    }
}
