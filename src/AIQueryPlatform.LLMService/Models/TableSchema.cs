using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models
{
    public class TableSchema
    {
        public string TableName { get; set; }
        public List<ColumnSchema> Columns { get; set; } = new();
    }
}
