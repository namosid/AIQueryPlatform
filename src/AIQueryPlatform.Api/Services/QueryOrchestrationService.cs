using AIQueryPlatform.Api.Models;
using AIQueryPlatform.Api.Models.DTOs;
using AIQueryPlatform.Api.Services.Interfaces;
using AIQueryPlatform.LLMServiceOperator;
using AIQueryPlatform.LLMServiceOperator.Interface;
using AIQueryPlatform.LLMServiceOperator.Models;
using System.Diagnostics;

namespace AIQueryPlatform.Api.Services;

/// <summary>
/// Orchestrates the entire query execution pipeline with streaming support
/// </summary>
public class QueryOrchestrationService : IQueryOrchestrationService
{
    private readonly TenantContext _tenantContext;
    private readonly INLToSqlService _nlToSqlService;
    private readonly ISqlValidatorService _sqlValidatorService;
    private readonly IQueryExecutionService _queryExecutionService;
    private readonly ISchemaService _schemaService;
    private readonly IIntelligenceLayerService _intelligenceLayerService;
    private readonly ILogger<QueryOrchestrationService> _logger;
    private readonly int _maxRowLimit;
    private readonly ILLMServicePipe _llmServicePipe;

    public QueryOrchestrationService(
        TenantContext tenantContext,
        INLToSqlService nlToSqlService,
        ISqlValidatorService sqlValidatorService,
        IQueryExecutionService queryExecutionService,
        ISchemaService schemaService,
        IIntelligenceLayerService intelligenceLayerService,
        IConfiguration configuration,
        ILogger<QueryOrchestrationService> logger,
        ILLMServicePipe llmServicePipe)
    {
        _tenantContext = tenantContext;
        _nlToSqlService = nlToSqlService;
        _sqlValidatorService = sqlValidatorService;
        _queryExecutionService = queryExecutionService;
        _schemaService = schemaService;
        _intelligenceLayerService = intelligenceLayerService;
        _logger = logger;
        _maxRowLimit = configuration.GetValue<int>("QueryExecution:MaxRowLimit", 100);
        _llmServicePipe = llmServicePipe;
    }

    public async IAsyncEnumerable<StreamEvent> ExecuteQueryStreamAsync(string query, string? conversationId = null)
    {
        if (!_tenantContext.HasTenant)
        {
            yield return new StreamEvent
            {
                Type = "error",
                Message = "No tenant context available"
            };
            yield break;
        }

        var tenant = _tenantContext.CurrentTenant!;
        var stopwatch = Stopwatch.StartNew();

        // Step 1: Log initial query
        if (!string.IsNullOrWhiteSpace(conversationId))
        {
            _logger.LogInformation("Processing query for tenant {TenantId} in conversation {ConversationId}: {Query}", 
                tenant.TenantId, conversationId, query);
        }
        else
        {
            _logger.LogInformation("Processing query for tenant {TenantId}: {Query}", tenant.TenantId, query);
        }

        yield return new StreamEvent
        {
            Type = "log",
            Message = "Processing your query..."
        };

        // Step 2: Load schema
        yield return new StreamEvent
        {
            Type = "log",
            Message = "Loading database schema..."
        };

        DatabaseSchema? schema = null;
        Exception? schemaError = null;

        try
        {
            schema = await _schemaService.GetDatabaseSchemaAsync(tenant.ConnectionString, tenant.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading schema for tenant {TenantId}", tenant.TenantId);
            schemaError = ex;
        }

        if (schemaError != null)
        {
            yield return new StreamEvent
            {
                Type = "error",
                Message = schemaError.Message
            };
            yield break;
        }

        // Step 3: Convert NL to SQL
        yield return new StreamEvent
        {
            Type = "log",
            Message = "Converting natural language to SQL..."
        };

        string? sql = null;
        Exception? sqlError = null;

        try
        {
            sql = await _nlToSqlService.ConvertNaturalLanguageToSqlAsync(query, schema!, tenant.DatabaseType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting NL to SQL for tenant {TenantId}", tenant.TenantId);
            sqlError = ex;
        }

        if (sqlError != null)
        {
            yield return new StreamEvent
            {
                Type = "error",
                Message = sqlError.Message
            };
            yield break;
        }

        yield return new StreamEvent
        {
            Type = "sql_generated",
            Sql = sql,
            Message = "SQL query generated"
        };

        // Step 4: Validate SQL
        yield return new StreamEvent
        {
            Type = "log",
            Message = "Validating SQL query..."
        };

        if (sql == null)
        {
            yield return new StreamEvent
            {
                Type = "error",
                Message = "SQL validation failed: SQL generation returned null"
            };
            yield break;
        }

        // if (!_sqlValidatorService.IsValidSelectQuery(sql, out var validationError))
        // {
        //     yield return new StreamEvent
        //     {
        //         Type = "error",
        //         Message = $"SQL validation failed: {validationError}"
        //     };
        //     yield break;
        // }

        // Step 5: Enforce row limit
        sql = _sqlValidatorService.EnforceRowLimit(sql, _maxRowLimit);

        // Step 6: Execute query
        yield return new StreamEvent
        {
            Type = "execution_progress",
            Message = "Executing query against database..."
        };

        QueryResult? result = null;
        Exception? executionError = null;

        try
        {
            result = await _queryExecutionService.ExecuteQueryAsync(sql!, tenant.ConnectionString, tenant.DatabaseType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query for tenant {TenantId}", tenant.TenantId);
            executionError = ex;
        }

        if (executionError != null)
        {
            yield return new StreamEvent
            {
                Type = "error",
                Message = executionError.Message
            };
            yield break;
        }

        if (result == null)
        {
            yield return new StreamEvent
            {
                Type = "error",
                Message = "Query execution returned no results"
            };
            yield break;
        }

        yield return new StreamEvent
        {
            Type = "log",
            Message = $"Query executed successfully. {result.RowCount} rows returned."
        };

        // Step 7: Determine visualization type
        var visualizationType = _intelligenceLayerService.DetermineVisualizationType(query, result);

        yield return new StreamEvent
        {
            Type = "log",
            Message = $"Recommended visualization: {visualizationType}"
        };

        // Step 8: Generate chart data if suitable
        ChartData? chartData = null;
        if (visualizationType == VisualizationType.Chart)
        {
            chartData = _intelligenceLayerService.ConvertToChartData(result);
            if (chartData == null)
            {
                // Fallback to table if chart conversion fails
                visualizationType = VisualizationType.Table;
                yield return new StreamEvent
                {
                    Type = "log",
                    Message = "Chart conversion failed, using table visualization"
                };
            }
            else
            {
                yield return new StreamEvent
                {
                    Type = "log",
                    Message = $"Chart data generated: {chartData.Labels.Count} labels, {chartData.Datasets.Count} series"
                };
            }
        }

        // Step 9: Return final result
        stopwatch.Stop();

        yield return new StreamEvent
        {
            Type = "final_result",
            Data = result,
            VisualizationType = visualizationType,
            ChartData = chartData,
            Message = $"Query completed in {stopwatch.ElapsedMilliseconds}ms"
        };

        _logger.LogInformation("Query completed successfully for tenant {TenantId} in {ElapsedMs}ms",
            tenant.TenantId, stopwatch.ElapsedMilliseconds);
    }

    public async Task<QueryResponse> ExecuteQueryAsync(string query, string? conversationId = null)
    {
        if (!_tenantContext.HasTenant)
        {
            return new QueryResponse
            {
                Success = false,
                ErrorMessage = "No tenant context available"
            };
        }

        var tenant = _tenantContext.CurrentTenant!;
        var stopwatch = Stopwatch.StartNew();
        var result = new QueryResult();
        // Generate chart data if suitable
        ChartData? chartData = null;
        try
        {
            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                _logger.LogInformation("Processing query for tenant {TenantId} in conversation {ConversationId}: {Query}", 
                    tenant.TenantId, conversationId, query);
            }
            else
            {
                _logger.LogInformation("Processing query for tenant {TenantId}: {Query}", tenant.TenantId, query);
            }

            // Load schema
            //var schema = await _schemaService.GetDatabaseSchemaAsync(tenant.ConnectionString, tenant.TenantId);

            // Convert NL to SQL
            //var sql = await _nlToSqlService.ConvertNaturalLanguageToSqlAsync(query, schema, tenant.DatabaseType);
            TenantData td = new TenantData
            {
                TenantDB = tenant.ConnectionString,
                TenantId = tenant.TenantId.ToString(),
                TenantName = tenant.Name,
                SchemaFile = tenant.SchemaFile,
                MappingFile = tenant.MappingFile,
                ConversationID = conversationId ?? Guid.NewGuid().ToString()
            };

            // Using the new LLMServicePipe to process the query through the entire pipeline
            var response = await _llmServicePipe.ProcessQuery(query, td, conversationId);
            var visualizationType = VisualizationType.TEXT; // Default to chart, will adjust based on response

            if (response.Type == ResponseType.SQL)
            {
                string sql = response.SQL;
                //Validate SQL
                // if (!_sqlValidatorService.IsValidSelectQuery(sql, out var validationError))
                // {
                //     return new QueryResponse
                //     {
                //         Success = false,
                //         ErrorMessage = $"SQL validation failed: {validationError}",
                //         GeneratedSql = sql
                //     };
                // }

                // Enforce row limit
                sql = _sqlValidatorService.EnforceRowLimit(sql, _maxRowLimit);

                // Execute query
                result = await _queryExecutionService.ExecuteQueryAsync(sql, tenant.ConnectionString, tenant.DatabaseType);

                // Determine visualization type
                visualizationType = _intelligenceLayerService.DetermineVisualizationType(query, result);


                if (visualizationType == VisualizationType.Chart)
                {
                    chartData = _intelligenceLayerService.ConvertToChartData(result);
                    if (chartData == null)
                    {
                        // Fallback to table if chart conversion fails
                        visualizationType = VisualizationType.Table;
                    }
                }

            }
            stopwatch.Stop();

            _logger.LogInformation("Query completed successfully for tenant {TenantId} in {ElapsedMs}ms",
                tenant.TenantId, stopwatch.ElapsedMilliseconds);

            return new QueryResponse
            {
                Success = true,
                GeneratedSql = response.Type == ResponseType.SQL ? response.SQL : response.Message,
                Result = result,
                VisualizationType = visualizationType,
                ChartData = chartData,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing query for tenant {TenantId}", tenant.TenantId);
            stopwatch.Stop();

            return new QueryResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }
}
