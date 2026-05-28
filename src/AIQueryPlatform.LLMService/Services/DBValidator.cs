using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.SqlValidator.Models;
using AIQueryPlatform.SqlValidator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class DBValidator
    {
        private readonly DatabaseSchema _schema;
        private readonly string _connectionString;
        public DBValidator(string llmSchema, string connectionString)
        {
            _schema = SqlValidator.Services.SchemaParser.Parse(llmSchema);
            _connectionString = connectionString;
        }

        public async Task<PipelineResult> ValidateQuery(string sql, string error, MemoryTurn mTurn)
        {
            HttpClient httpClient = null;
            // 3. Build the pipeline
            //    Pass connectionString for Layer 4 dry-run, or null to skip it
            var pipeline = new SqlValidationPipeline(
                _schema,
                httpClient,
                connectionString: _connectionString
            );
            TurnState st = TurnState.RefinedQuery;
            int number = 1;
            if (mTurn != null)
            {
                number = mTurn.TurnNumber;
                st = Enum.TryParse<TurnState>(mTurn.Type.ToString(), out var parsedState) ? parsedState : TurnState.SQLError;
            }
            var turn = new STMTurn
            {
                TurnNumber = number,
                State = st,
                UserInput = error,
                GeneratedSQL = sql
            };

            var result = await pipeline.ValidateAsync(sql, turn);
            return result;
        }
    }
}
