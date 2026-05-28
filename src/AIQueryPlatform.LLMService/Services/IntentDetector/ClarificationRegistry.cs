using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.IntentDetector
{
    public class ClarificationRegistry
    {
        private readonly List<(string tableName, string content)> _schemaChunks;

        // Maps table name patterns → friendly display info
        private static readonly List<ClarificationMapping> Mappings = new()
    {
        new() { Pattern = "Attendance",   Label = "Attendance Info",
                Detail  = "Present days, Absent days, Attendance %" },

        new() { Pattern = "Fee",          Label = "Fee Info",
                Detail  = "Fee paid, Pending amount, Due dates" },

        new() { Pattern = "ExamResults",         Label = "Marks Info",
                Detail  = "Subject marks, Grade, Total, Percentage" },

         new() { Pattern = "Examinations",         Label = "Exam Info",
                Detail  = "Exam name, Date, Score, Result" },

        new() { Pattern = "Parent",       Label = "Parent Info",
                Detail  = "Father, Mother name, phone, email" },

        new() { Pattern = "Enrollment",   Label = "Academic Info",
                Detail  = "Class, Section, RollNumber, AdmissionDate" },

        new() { Pattern = "Transport",    Label = "Transport Info",
                Detail  = "Route, Bus number, Stop, Driver" },

        new() { Pattern = "Library",      Label = "Library Info",
                Detail  = "Books issued, Return date, Fine" },

        new() { Pattern = "Hostel",       Label = "Hostel Info",
                Detail  = "Room number, Block, Joining date" },

        new() { Pattern = "Document",     Label = "Documents Info",
                Detail  = "Certificates, ID proof, uploaded files" },
    };

        public ClarificationRegistry(List<(string tableName, string content)> schemaChunks)
        {
            _schemaChunks = schemaChunks;
        }

        /// <summary>
        /// Dynamically build options based on what tables
        /// actually exist in YOUR schema
        /// </summary>
        public List<ClarificationOption> BuildOptions(string entity)
        {
            var options = new List<ClarificationOption>();
            int key = 1;

            // Always add Personal Info first (from main entity table)
            options.Add(new ClarificationOption
            {
                Key = (key++).ToString(),
                Label = "Personal Info",
                Detail = "FirstName, LastName, DOB, Gender, BloodGroup, Aadhar",
                Tables = new List<string> { entity + "s" }  // "Students"
            });

            // Scan schemaChunks — only add options for tables that EXIST
            foreach (var mapping in Mappings)
            {
                // Check if any chunk matches this pattern for this entity
                var matchingTables = _schemaChunks
                 .Where(c => c.tableName.Contains(mapping.Pattern,
                         StringComparison.OrdinalIgnoreCase)
                     &&
                     // Exclude tables that clearly belong to a DIFFERENT entity
                     // (e.g. StaffAttendance when entity is Student)
                     // But allow tables that are neutral (FeePayment, ExamResults)
                     (!ContainsDifferentEntity(c.tableName, entity)
                      || IsSharedTable(c.tableName)))
                 .Select(c => c.tableName)
                 .ToList();

                if (!matchingTables.Any()) continue; // ← skip if table doesn't exist

                options.Add(new ClarificationOption
                {
                    Key = (key++).ToString(),
                    Label = mapping.Label,
                    Detail = mapping.Detail,
                    Tables = matchingTables  // actual table names from schema
                });

                Console.WriteLine($"[ClarificationRegistry] Added option '{mapping.Label}' → {string.Join(",", matchingTables)}");
            }

            // Always add "All Info" last
            options.Add(new ClarificationOption
            {
                Key = (key++).ToString(),
                Label = "All Info",
                Detail = "Complete profile",
                Tables = options.SelectMany(o => o.Tables).Distinct().ToList()
            });

            return options;
        }

        // Tables shared between Student and Staff
        private static bool IsSharedTable(string tableName) =>
            new[] { "Classes", "Sections", "Subjects" }
                .Any(t => tableName.Contains(t, StringComparison.OrdinalIgnoreCase));

        private bool ContainsDifferentEntity(string tableName, string entity)
        {
            // Known entity prefixes in your schema
            var knownEntities = new[] { "Student", "Staff", "Teacher", "Parent", "ExamResults" , "FeeInvoices" };

            return knownEntities
                .Where(e => !e.Equals(entity, StringComparison.OrdinalIgnoreCase))
                .Any(e => tableName.Contains(e, StringComparison.OrdinalIgnoreCase));
        }
    }

    public class ClarificationMapping
    {
        public string Pattern { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public class ClarificationOption
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public List<string> Tables { get; set; } = new(); // ← actual table names
    }
}
