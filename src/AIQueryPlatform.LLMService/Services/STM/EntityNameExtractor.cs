using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Tools;
using AIQueryPlatform.LLMServiceOperator.Services.Qdrant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.STM
{
    public static class EntityNameExtractor
    {
        // ── Pronouns that reference a previously resolved entity ─────────────
        public static readonly HashSet<string> Pronouns = new(StringComparer.OrdinalIgnoreCase)
        {
            "him", "her", "his", "hers",
            "them", "their", "they",
            "he", "she", "it", "its"
        };

        // ── EntityType mirrors the logical types used in ExtractedEntity ─────
        public enum EntityType
        {
            // ── People ────────────────────────────────────────────────────────
            Student,
            Staff,
            Teacher,

            // ── Academic Structure ────────────────────────────────────────────
            Class,
            Section,
            Subject,
            Syllabus,
            Timetable,
            Examination,

            // ── Attendance ────────────────────────────────────────────────────
            Attendance,

            // ── Finance ───────────────────────────────────────────────────────
            Fees,
            FeeCollection,

            // ── Documents ────────────────────────────────────────────────────
            StudentDocument,
            StaffDocument,

            // ── Transport ────────────────────────────────────────────────────
            Transport,
            Route,

            // ── Hostel ───────────────────────────────────────────────────────
            Hostel,

            // ── Library ──────────────────────────────────────────────────────
            Library,

            // ── Communication ────────────────────────────────────────────────
            Notice,
            Event,

            // ── System / STM ─────────────────────────────────────────────────
            Unknown,
            Pronoun
        }

        public record ExtractedEntity(
            EntityType Type,
            string? Name,
            bool IsPronoun,   // true = needs resolution from prior context
            bool IsScope      // true = class/section level, not individual
        );

        // ── Maps EntityDetector string types → EntityNameExtractor.EntityType ─
        private static readonly Dictionary<string, EntityType> TypeMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Student",  EntityType.Student  },
                { "Teacher",  EntityType.Teacher  },
                { "Staff",    EntityType.Staff    },
                { "Class",    EntityType.Class    },
                { "Section",  EntityType.Section  },
            };

        // ────────────────────────────────────────────────────────────────────
        // Extract — driven entirely by EntityDetector.Rules (single source of truth)
        //
        // Priority order matches the original Extract() contract:
        //   1. Student ID  (Student\d+)       — highest specificity
        //   2. Named student / roll / admission / enrollment
        //   3. Teacher (named then generic)
        //   4. Staff  (named, emp-id, then generic)
        //   5. Class
        //   6. Section
        //   7. Pronoun
        //   8. Unknown
        // ────────────────────────────────────────────────────────────────────
        public static ExtractedEntity Extract(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ExtractedEntity(EntityType.Unknown, null, false, false);

            // ── 1. Pronoun check (quick, before heavy rule scan) ─────────────
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Any(w => Pronouns.Contains(w)))
                return new ExtractedEntity(EntityType.Unknown, null, IsPronoun: true, IsScope: false);

            // ── 2. Walk EntityDetector.Rules in order ────────────────────────
            //    First rule that (a) matches AND (b) maps to a known STM type wins.
            foreach (var (pattern, entityType, isScope) in EntityDetector.Rules)
            {
                // Only care about types that have STM meaning
                if (!TypeMap.TryGetValue(entityType, out var stmType))
                    continue;

                var match = pattern.Match(query);
                if (!match.Success)
                    continue;

                // Extract the identity value (named group "value" or full match)
                var value = EntityDetector.ExtractValue(match);

                // For generic/domain-signal rules (e.g. bare "students?"), value
                // will equal the keyword itself — not a real name/ID. Treat as null.
                var name = IsKeyword(value, entityType) ? null : value;

                return new ExtractedEntity(stmType, name, IsPronoun: false, IsScope: isScope);
            }

            // ── 3. Unknown ───────────────────────────────────────────────────
            return new ExtractedEntity(EntityType.Unknown, null, false, false);
        }

        // ── ResolveEntity — unchanged logic, now calls the unified Extract ───
        public static ExtractedEntity ResolveEntity(
            string originalPrompt,
            MemoryTurn? lastTurn)
        {
            var extracted = Extract(originalPrompt);

            // Case 1 — explicit entity with a real name/ID found
            if (!extracted.IsPronoun && extracted.Name != null)
            {
                var validation = EnumValidator.Validate(extracted.Type, extracted.Name);

                if (validation.IsValid)
                {
                    // PassThrough or matched — use resolved value
                    return extracted with { Name = validation.ResolvedValue };
                }
                // No enum match — log and fall through to clarification
                Helper.LogMessage(string.Format(
                    "Enum validation failed for {0} = '{1}'", extracted.Type, extracted.Name));
                return extracted with { Name = null };   // triggers clarification upstream
            }

            // Case 2 — pronoun: resolve from last turn
            var words = originalPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasPronoun = words.Any(w => Pronouns.Contains(w));
            if (hasPronoun && lastTurn?.ResolvedEntity != null)
                return lastTurn.ResolvedEntity with { IsPronoun = false };

            // Case 3 — continuation query → resolve from last turn
            if (extracted.Name == null && lastTurn?.ResolvedEntity != null)
            {
                Helper.LogMessage(string.Format(
                    "Continuation query detected — resolved entity from last turn: {0} = {1}",
                    lastTurn.ResolvedEntity.Type,
                    lastTurn.ResolvedEntity.Name));
                return lastTurn.ResolvedEntity with { IsPronoun = false };
            }

            // Case 4 — nothing resolved
            Helper.LogMessage(string.Format(
                "No entity resolved for: '{0}'", originalPrompt));
            return extracted;
        }

        // ── Decides whether the extracted value is just a trigger keyword ────
        // e.g. "students", "teacher", "staff" are not real identity values.
        // We compare lower-cased value against known keyword stems per type.
        private static readonly Dictionary<string, HashSet<string>> KeywordStemsByType =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Student", new(StringComparer.OrdinalIgnoreCase)
                    { "student", "students" } },
                { "Teacher", new(StringComparer.OrdinalIgnoreCase)
                    { "teacher", "teachers" } },
                { "Staff",   new(StringComparer.OrdinalIgnoreCase)
                    { "staff", "employee", "employees", "emp" } },
                { "Class",   new(StringComparer.OrdinalIgnoreCase)
                    { "class", "classes", "grade", "std", "batch" } },
                { "Section", new(StringComparer.OrdinalIgnoreCase)
                    { "section" } },
            };

        private static bool IsKeyword(string? value, string entityType)
        {
            if (value == null) return true;
            return KeywordStemsByType.TryGetValue(entityType, out var stems)
                && stems.Contains(value);
        }


    }
}