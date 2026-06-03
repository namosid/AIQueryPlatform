using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AIQueryPlatform.LLMServiceOperator.Services.STM.EntityNameExtractor;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    public class EnumValidationResult
    {
        public bool IsValid { get; private set; }
        public bool IsPassThrough { get; private set; }  // no enum defined = always valid
        public EntityType? EntityType { get; private set; }
        public string? Column { get; private set; }
        public string? RawValue { get; private set; }
        public string? ResolvedValue { get; private set; }

        // ── Factory methods ───────────────────────────────────────────────
        public static EnumValidationResult Valid(string column, string resolvedValue) => new()
        {
            IsValid = true,
            IsPassThrough = false,
            Column = column,
            ResolvedValue = resolvedValue
        };

        public static EnumValidationResult PassThrough(string rawValue) => new()
        {
            IsValid = true,
            IsPassThrough = true,
            ResolvedValue = rawValue
        };

        public static EnumValidationResult Invalid(EntityType entityType, string rawValue) => new()
        {
            IsValid = false,
            EntityType = entityType,
            RawValue = rawValue,
            ResolvedValue = null
        };
    }
}
