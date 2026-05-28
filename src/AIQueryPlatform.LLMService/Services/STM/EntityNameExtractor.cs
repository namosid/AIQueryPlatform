using AIQueryPlatform.LLMServiceOperator.Models.STM;
using AIQueryPlatform.LLMServiceOperator.Tools;
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
        // Pronouns that reference a previously resolved entity
        public static readonly HashSet<string> Pronouns = new(StringComparer.OrdinalIgnoreCase)
        {
            "him", "her", "his", "hers",
            "them", "their", "they",
            "he", "she", "it", "its"
        };

        public enum EntityType
        {
            Student, Staff, Teacher, Class, Section, Unknown, Pronoun
        }

        public record ExtractedEntity(
            EntityType Type,
            string? Name,
            bool IsPronoun,   // true = needs resolution from prior context
            bool IsScope      // true = class/section level, not individual
        );

        public static ExtractedEntity Extract(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new ExtractedEntity(EntityType.Unknown, null, false, false);

            // ── 1. STUDENT ID — check first before anything else ─────────
            // Matches: "Student15", "Student10", "Student123"
            // "Get Student Personal Details name Student15" → "Student15" ✅
            var studentIDMatch = Regex.Match(query,
                @"\b(Student\d+)\b",
                RegexOptions.IgnoreCase);

            if (studentIDMatch.Success)
                return new ExtractedEntity(
                    EntityType.Student,
                    studentIDMatch.Groups[1].Value.Trim(),
                    IsPronoun: false,
                    IsScope: false);

            // ── 2. STUDENT — "student name/named John" ────────────────────
            // Only if no StudentID found above
            // Matches: "student named John", "for student John"
            var studentNamedMatch = Regex.Match(query,
                @"\bstudent\s+(?:named\s+|name\s+)([A-Za-z][A-Za-z0-9]*)\b",
                RegexOptions.IgnoreCase);

            if (studentNamedMatch.Success)
                return new ExtractedEntity(
                    EntityType.Student,
                    studentNamedMatch.Groups[1].Value.Trim(),
                    IsPronoun: false,
                    IsScope: false);

            // ── 3. STUDENT — "for student John" ──────────────────────────
            var studentForMatch = Regex.Match(query,
                @"\b(?:for\s+|of\s+)student\s+([A-Za-z][A-Za-z0-9]*)\b",
                RegexOptions.IgnoreCase);

            if (studentForMatch.Success)
                return new ExtractedEntity(
                    EntityType.Student,
                    studentForMatch.Groups[1].Value.Trim(),
                    IsPronoun: false,
                    IsScope: false);

            // ── 4. STAFF / TEACHER ────────────────────────────────────────
            var staffMatch = Regex.Match(query,
                @"\b(?:for\s+|of\s+)?(?:staff|teacher)\b\s+(?:named\s+|name\s+)?([A-Za-z0-9]+)\b",
                RegexOptions.IgnoreCase);
            if (staffMatch.Success)
            {
                var type = Regex.IsMatch(query, @"\bteacher\b", RegexOptions.IgnoreCase)
                    ? EntityType.Teacher : EntityType.Staff;
                return new ExtractedEntity(
                    type,
                    staffMatch.Groups[1].Value.Trim(),
                    false, false);
            }

            // ── 5. CLASS ─────────────────────────────────────────────────
            var classMatch = Regex.Match(query,
                @"\b(?:class(?:\s+name)?\s+)?([A-Za-z]+\s*\d+[A-Za-z]?)(?:\s+class)?\b",
                RegexOptions.IgnoreCase);
            if (classMatch.Success &&
                Regex.IsMatch(query, @"\b(?:class|grade)\b", RegexOptions.IgnoreCase))
                return new ExtractedEntity(
                    EntityType.Class,
                    classMatch.Groups[1].Value.Trim(),
                    IsPronoun: false,
                    IsScope: true);

            // ── 6. SECTION ───────────────────────────────────────────────
            var sectionMatch = Regex.Match(query,
                @"\bsection\s+([A-Za-z0-9]{1,10})\b",
                RegexOptions.IgnoreCase);
            if (sectionMatch.Success)
                return new ExtractedEntity(
                    EntityType.Section,
                    sectionMatch.Groups[1].Value.Trim(),
                    IsPronoun: false,
                    IsScope: true);

            // ── 7. PRONOUN ───────────────────────────────────────────────
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Any(w => Pronouns.Contains(w)))
                return new ExtractedEntity(
                    EntityType.Unknown, null, IsPronoun: true, IsScope: false);

            // ── 8. UNKNOWN ───────────────────────────────────────────────
            return new ExtractedEntity(EntityType.Unknown, null, false, false);
        }

        public static ExtractedEntity ResolveEntity(
        string originalPrompt,
        MemoryTurn? lastTurn)
        {
            var extracted = EntityNameExtractor.Extract(originalPrompt);

            // Case 1
            if (!extracted.IsPronoun && extracted.Name != null)
                return extracted;

            // Case 2
            var words = originalPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool hasPronoun = words.Any(w => EntityNameExtractor.Pronouns.Contains(w));

            if (hasPronoun && lastTurn?.ResolvedEntity != null)
                return lastTurn.ResolvedEntity with { IsPronoun = false };

            // Case 3 — "include attendance information" lands here
            if (extracted.Name == null && lastTurn?.ResolvedEntity != null)
            {
                Helper.LogMessage(String.Format(
                    "Continuation query detected — resolved entity from last turn: {0} = {1}",
                    lastTurn.ResolvedEntity.Type,
                    lastTurn.ResolvedEntity.Name));

                return lastTurn.ResolvedEntity with { IsPronoun = false };
            }

            // Case 4
            Helper.LogMessage(string.Format("No entity resolved for: '{0}'", originalPrompt));
            return extracted;
        }
    }
}
