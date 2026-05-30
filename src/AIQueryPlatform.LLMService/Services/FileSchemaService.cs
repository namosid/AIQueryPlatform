using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using AIQueryPlatform.LLMServiceOperator.Models;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class FileSchemaService
    {
        private readonly IConfiguration _config;

        public FileSchemaService(IConfiguration config)
        {
            _config = config;
        }


        public string ReadSchema(string filename)
        {
            string relativePath = Convert.ToString(_config["SchemaSettings:SchemaPath"]);

            var fullPath = Path.Combine(relativePath, "SchemaFiles//" + filename + ".txt");

            if (!File.Exists(fullPath))
                throw new Exception($"Schema file not found at: {fullPath}");

            return File.ReadAllText(fullPath);
        }
    }
}
