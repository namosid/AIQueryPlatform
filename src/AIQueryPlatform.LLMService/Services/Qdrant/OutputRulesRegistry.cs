using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    // OutputRulesRegistry.cs
    public static class OutputRulesRegistry
    {
        // Key: trigger keyword (matched against user query entities)
        // Value: tables that MUST always be included in schema
        public static readonly Dictionary<string, List<string>> RequiredTables = new()
        {
            ["Students"] = new List<string> { "Students", "Sections", "Classes" },
            ["Staff"] = new List<string> { "Staff", "Departments" },
            // Add more entity rules here
        };

        // The mandatory fields per entity (for prompt injection)
        public static readonly Dictionary<string, string> MandatoryFields = new()
        {
            ["Students"] =
                "ALWAYS include in SELECT: StudentID, AdmissionNumber, FirstName, LastName. " +
                "OPTIONAL if relevant: ClassName (from Classes), SectionName (from Sections), RollNumber.",
            ["Staff"] =
                "ALWAYS include in SELECT: StaffID, EmployeeCode, FirstName, LastName.",
        };
    }
}
