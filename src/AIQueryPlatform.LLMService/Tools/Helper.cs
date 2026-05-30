using AIQueryPlatform.LLMServiceOperator.Models.STM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Tools
{
    public static class Helper
    {
        public static void LogMessage(string msg)
        {
            Console.WriteLine(msg);
        }

        // Option 1 — Store as JSON string (simple)
        public static string SerializeVector(float[] vector)
        {
            return JsonSerializer.Serialize(vector);
            // "[0.123, 0.456, 0.789, ...]"
        }

        // Option 2 — Store as binary (compact, faster search)
        public static byte[] SerializeVectorInByte(float[] vector)
        {
            var bytes = new byte[vector.Length * 4];
            Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static QueryType ParseQueryType(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return QueryType.Unknown;

            if (Enum.TryParse<QueryType>(value, ignoreCase: true, out var result))
                return result;

            LogMessage(String.Format("Unrecognized QueryType value: '{value}'", value));
            return QueryType.Unknown;
        }

        // ── Helper: safely deserialize stored params ────────────────────────
        public static Dictionary<string, string> DeserializeParams(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new();
            try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new(); }
            catch { return new(); }
        }
    }
}
