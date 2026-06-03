using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.Qdrant
{
    public static class EntityDetector
    {
        // ── Rule tuple ───────────────────────────────────────────────────────
        // Pattern     : regex to match the entity phrase
        // EntityType  : logical category string ("Student", "Staff", …)
        // ValueGroup  : named capture group "value" in the pattern that holds
        //               the identity token (ID / name / number).
        //               null = the whole match IS the value (domain-signal rules).
        // IsScope     : true for class/section-level entities (not individuals)
        // ────────────────────────────────────────────────────────────────────
        public static readonly List<(Regex Pattern, string EntityType, bool IsScope)> Rules = new()
        {
            // ── Student ──────────────────────────────────────────────────────────

            // "Student15", "Student10"  → value = "Student15"
            (new Regex(@"\b(?<value>Student\d+)\b",
                RegexOptions.IgnoreCase), "Student", false),

            // "student named John", "student name John" → value = "John"
            (new Regex(@"\bstudent\s+(?:named\s+|name\s+)(?<value>[A-Za-z][A-Za-z0-9]*)\b",
                RegexOptions.IgnoreCase), "Student", false),

            // "for student John", "of student John" → value = "John"
            (new Regex(@"\b(?:for\s+|of\s+)student\s+(?<value>[A-Za-z][A-Za-z0-9]*)\b",
                RegexOptions.IgnoreCase), "Student", false),

            // "roll no 42", "roll number 7" → value = "42"
            (new Regex(@"\broll\s*(?:no|number|#)?\s*(?<value>\d+)\b",
                RegexOptions.IgnoreCase), "Student", false),

            // "admission no A1234" → value = "A1234"
            (new Regex(@"\badmission\s*(?:no|number|#)?\s*(?<value>[A-Z0-9]+)\b",
                RegexOptions.IgnoreCase), "Student", false),

            // "enrollment no 5001" → value = "5001"
            (new Regex(@"\benroll(?:ment)?\s*(?:no|number|#)?\s*(?<value>[A-Z0-9]+)\b",
                RegexOptions.IgnoreCase), "Student", false),

            // generic "students", "a student" — domain signal only, no value
            (new Regex(@"\bstudents?\b",
                RegexOptions.IgnoreCase), "Student", false),

            // ── Staff / Teacher ──────────────────────────────────────────────────

            // "teacher named Priya", "for teacher Priya" → value = "Priya"
            (new Regex(@"\b(?:for\s+|of\s+)?teacher\s+(?:named\s+|name\s+)?(?<value>[A-Za-z0-9]+)\b",
                RegexOptions.IgnoreCase), "Teacher", false),

            // generic "teachers", "a teacher" — domain signal only
            (new Regex(@"\bteachers?\b",
                RegexOptions.IgnoreCase), "Teacher", false),

            // "staff named Ravi", "for staff Ravi" → value = "Ravi"
            (new Regex(@"\b(?:for\s+|of\s+)?staff\s+(?:named\s+|name\s+)?(?<value>[A-Za-z0-9]+)\b",
                RegexOptions.IgnoreCase), "Staff", false),

            // "staff id S10", "staff member" — domain signal
            (new Regex(@"\bstaff\b",
                RegexOptions.IgnoreCase), "Staff", false),

            // "employee code E101", "emp id 101" → value = "E101" / "101"
            (new Regex(@"\bemp(?:loyee)?\s*(?:id|code|no|#)\s*(?<value>[A-Z0-9]+)\b",
                RegexOptions.IgnoreCase), "Staff", false),

            // generic "employees", "emp" — domain signal
            (new Regex(@"\bemp(?:loyee)?s?\b",
                RegexOptions.IgnoreCase), "Staff", false),

            // ── Class / Section ──────────────────────────────────────────────────

            // "Class 9A", "Grade 9A", "Std 8B" → ClassValue = "Grade 9", SectionValue = "A"
            (new Regex(@"\b(?:class|grade|std)\s*(?<ClassValue>[1-9][0-9]?)\s*(?<SectionValue>[A-Za-z])?\b",
                RegexOptions.IgnoreCase), "Class", true),

            // "Class Grade 9A" → skip "Class", fire on "Grade 9A"
            // (keep the negative lookahead fix from before)
            (new Regex(@"\b(?:class|grade|std)\s*(?!(?:class|grade|std)\b)(?<ClassValue>[1-9][0-9]?)\s*(?<SectionValue>[A-Za-z])?\b",
                RegexOptions.IgnoreCase), "Class", true),

            // generic "classes"
            (new Regex(@"\bclasses\b",
                RegexOptions.IgnoreCase), "Class", true),

            // "section A", "section B2" → value = "A" / "B2"
            (new Regex(@"\bsection\s+(?<value>[A-Za-z0-9]{1,10})\b",
                RegexOptions.IgnoreCase), "Section", true),

            // "batch 2023-24" → value = "2023-24"
            (new Regex(@"\bbatch\s+(?<value>20\d{2}(?:-\d{2,4})?)\b",
                RegexOptions.IgnoreCase), "Class", true),

            // ── Subject ──────────────────────────────────────────────────────────

            (new Regex(@"\b(?<value>Mathematics|Math|Physics|Chemistry|Biology|English|Hindi|" +
                       @"History|Geography|Science|Computer\s*Science|CS|EVS|" +
                       @"Economics|Accounts?|Accountancy|Sanskrit|Urdu|Civics|" +
                       @"Physical\s*Education|PE|Art|Music|Commerce)\b",
                       RegexOptions.IgnoreCase), "Subject", false),

            // ── Exam ─────────────────────────────────────────────────────────────

            (new Regex(@"\b(?<value>(?:unit\s*test|UT)\s*\d?)\b",
                RegexOptions.IgnoreCase), "Exam", false),

            (new Regex(@"\b(?<value>(?:FA|SA|PT)\s*\d)\b",
                RegexOptions.IgnoreCase), "Exam", false),

            (new Regex(@"\b(?<value>(?:half\s*yearly|annual|quarterly|pre\s*board|board|" +
                       @"mid\s*term|final|terminal)\s*(?:exam(?:ination)?s?)?)\b",
                       RegexOptions.IgnoreCase), "Exam", false),

            (new Regex(@"\bexams?\b",           RegexOptions.IgnoreCase), "Exam",       false),
            (new Regex(@"\bexaminations?\b",    RegexOptions.IgnoreCase), "Exam",       false),
            (new Regex(@"\btests?\b",           RegexOptions.IgnoreCase), "Exam",       false),

            // ── Attendance ────────────────────────────────────────────────────────

            (new Regex(@"\battendances?\b",                                RegexOptions.IgnoreCase), "Attendance", false),
            (new Regex(@"\b(?:absent|present|late|leave)\b",               RegexOptions.IgnoreCase), "Attendance", false),
            (new Regex(@"\b(?:medical|casual|earned|sick|half\s*day)\s*leave\b",
                RegexOptions.IgnoreCase), "Attendance", false),

            // ── Fee ───────────────────────────────────────────────────────────────

            (new Regex(@"\bfees?\b",                                       RegexOptions.IgnoreCase), "Fee",        false),
            (new Regex(@"\b(?:tuition|transport|hostel|library|sports|lab|" +
                       @"admission|annual|misc(?:ellaneous)?|development)\s*fee\b",
                       RegexOptions.IgnoreCase), "Fee",        false),
            (new Regex(@"\binvoices?\b",                                   RegexOptions.IgnoreCase), "Fee",        false),
            (new Regex(@"\bpayments?\b",                                   RegexOptions.IgnoreCase), "FeePayment", false),
            (new Regex(@"\breceipts?\b",                                   RegexOptions.IgnoreCase), "FeePayment", false),

            // ── Result / Report Card ──────────────────────────────────────────────

            (new Regex(@"\bresults?\b",                                    RegexOptions.IgnoreCase), "ExamResult", false),
            (new Regex(@"\bmarks?\b",                                      RegexOptions.IgnoreCase), "ExamResult", false),
            (new Regex(@"\bgrades?\b",                                     RegexOptions.IgnoreCase), "ExamResult", false),
            (new Regex(@"\breport\s*cards?\b",                             RegexOptions.IgnoreCase), "ReportCard", false),

            // ── Hostel ────────────────────────────────────────────────────────────

            (new Regex(@"\bhostels?\b",                                    RegexOptions.IgnoreCase), "Hostel",           false),
            (new Regex(@"\b(?:room|dorm(?:itory)?|hostel\s*block)\s+(?<value>[A-Z0-9]+)\b",
                RegexOptions.IgnoreCase), "HostelRoom",       false),
            (new Regex(@"\brooms?\b",                                      RegexOptions.IgnoreCase), "HostelRoom",       false),
            (new Regex(@"\ballocations?\b",                                RegexOptions.IgnoreCase), "HostelAllocation", false),

            // ── Transport ─────────────────────────────────────────────────────────

            (new Regex(@"\b(?:bus|transport|route)\b",                     RegexOptions.IgnoreCase), "Transport", false),
            (new Regex(@"\bbus\s*route\s*(?<value>\d+)\b",                 RegexOptions.IgnoreCase), "Transport", false),
            (new Regex(@"\broute\s*(?:no|#)?\s*(?<value>\d+)\b",           RegexOptions.IgnoreCase), "Transport", false),
            (new Regex(@"\bvehicles?\b",                                   RegexOptions.IgnoreCase), "Vehicle",   false),
            (new Regex(@"\b(?<value>[A-Z]{2}\d{2}[A-Z]{1,2}\d{4})\b"),                              "Vehicle",   false),

            // ── Parent ────────────────────────────────────────────────────────────

            (new Regex(@"\b(?:parents?|guardians?|father|mother)\b",       RegexOptions.IgnoreCase), "Parent", false),

            // ── Timetable ─────────────────────────────────────────────────────────

            (new Regex(@"\btimetables?\b",   RegexOptions.IgnoreCase), "Timetable", false),
            (new Regex(@"\bschedules?\b",    RegexOptions.IgnoreCase), "Timetable", false),
            (new Regex(@"\bperiods?\b",      RegexOptions.IgnoreCase), "Timetable", false),

            // ── Homework ──────────────────────────────────────────────────────────

            (new Regex(@"\bhomeworks?\b",    RegexOptions.IgnoreCase), "Homework",           false),
            (new Regex(@"\bassignments?\b",  RegexOptions.IgnoreCase), "Homework",           false),
            (new Regex(@"\bsubmissions?\b",  RegexOptions.IgnoreCase), "HomeworkSubmission", false),

            // ── Library ───────────────────────────────────────────────────────────

            (new Regex(@"\bbooks?\b",        RegexOptions.IgnoreCase), "Library",   false),
            (new Regex(@"\blibrary\b",       RegexOptions.IgnoreCase), "Library",   false),
            (new Regex(@"\bISBN\b",          RegexOptions.IgnoreCase), "Library",   false),
            (new Regex(@"\bissues?\b",        RegexOptions.IgnoreCase), "BookIssue", false),

            // ── Salary ────────────────────────────────────────────────────────────

            (new Regex(@"\bsalar(?:y|ies)\b",                                RegexOptions.IgnoreCase), "StaffSalary", false),
            (new Regex(@"\bpayroll\b",                                       RegexOptions.IgnoreCase), "StaffSalary", false),
            (new Regex(@"\b(?:HRA|DA|PF|TDS|allowances?|deductions?)\b",     RegexOptions.IgnoreCase), "StaffSalary", false),

            // ── Scholarship ───────────────────────────────────────────────────────

            (new Regex(@"\bscholarships?\b", RegexOptions.IgnoreCase), "Scholarship", false),
            (new Regex(@"\bdiscounts?\b",    RegexOptions.IgnoreCase), "Scholarship", false),

            // ── Assets ────────────────────────────────────────────────────────────

            (new Regex(@"\bassets?\b",       RegexOptions.IgnoreCase), "Asset", false),
            (new Regex(@"\bmaintenance\b",   RegexOptions.IgnoreCase), "Asset", false),

            // ── Events / Announcements ────────────────────────────────────────────

            (new Regex(@"\bevents?\b",           RegexOptions.IgnoreCase), "Event",        false),
            (new Regex(@"\bholidays?\b",         RegexOptions.IgnoreCase), "Event",        false),
            (new Regex(@"\bannouncements?\b",    RegexOptions.IgnoreCase), "Announcement", false),
            (new Regex(@"\bnotifications?\b",    RegexOptions.IgnoreCase), "Announcement", false),

            // ── Academic Year ─────────────────────────────────────────────────────

            (new Regex(@"\b(?<value>20\d{2}-(?:20)?\d{2})\b"),                              "AcademicYear", false),
            (new Regex(@"\bacademic\s*year\b",   RegexOptions.IgnoreCase), "AcademicYear", false),
            (new Regex(@"\bsession\b",           RegexOptions.IgnoreCase), "AcademicYear", false),

            // ── Department ────────────────────────────────────────────────────────

            (new Regex(@"\bdepartments?\b",  RegexOptions.IgnoreCase), "Department", false),
            (new Regex(@"\bdept\b",          RegexOptions.IgnoreCase), "Department", false),


            // ── Staff Document ────────────────────────────────────────────────────
            (new Regex(@"\bstaff\s*documents?\b",                          RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\b(?:appointment|offer|joining|relieving|experience|noc|termination)\s*letters?\b",
                RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\b(?:resume|cv|curriculum\s*vitae|bio\s*data)\b", RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\b(?:id\s*card|identity\s*card|employee\s*id|staff\s*id)\b",
                RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\b(?:contract|agreement|bond|nda|mou)\b",         RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\b(?:increment|promotion|transfer|suspension|warning|show\s*cause)\s*letters?\b",
                RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\b(?:payslip|salary\s*slip|pay\s*stub|salary\s*certificate)\b",
                RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\b(?:pf|provident\s*fund|gratuity|epf)\s*(?:form|document|letter)?\b",
                RegexOptions.IgnoreCase), "StaffDocument", false),
            (new Regex(@"\bstaff\s*(?:record|profile|file|detail|info(?:rmation)?)\b",
                RegexOptions.IgnoreCase), "StaffDocument", false),


            // ── Student Document ──────────────────────────────────────────────────
            (new Regex(@"\bstudent\s*documents?\b",                        RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:admission|enrollment|enrolment)\s*form\b",   RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:tc|transfer\s*certificate|leaving\s*certificate|lc|school\s*leaving)\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:bonafide|character|conduct|migration)\s*certificate\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:mark\s*sheet|marksheet|grade\s*card|report\s*card|progress\s*report)\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:id\s*card|identity\s*card|student\s*id|library\s*card)\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:birth\s*certificate|dob\s*proof|age\s*proof)\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:fee\s*receipt|payment\s*receipt|fee\s*certificate|scholarship)\s*(?:form|letter|document)?\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\bstudent\s*(?:record|profile|file|detail|info(?:rmation)?)\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:aadhar|aadhaar|pan|passport|ration\s*card)\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
            (new Regex(@"\b(?:admission|enrollment|enrolment)\s*(?:form|documents?|record|file|paper)s?\b",
                RegexOptions.IgnoreCase), "StudentDocument", false),
        };

        // ── Entity types that map to an individual person (used by STM) ─────
        // EntityNameExtractor uses this to decide whether to populate Name.
        public static readonly HashSet<string> IdentityTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Student", "Teacher", "Staff"
            };

        // ── Entity types that are scope/group level (used by STM) ───────────
        public static readonly HashSet<string> ScopeTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Class", "Section"
            };

        // ── Helpers to extract the matched value from a rule ─────────────────
        // Returns the named "value" group if present, else the full match.
        public static string? ExtractValue(Match match)
        {
            var g = match.Groups["value"];
            return g.Success ? g.Value.Trim() : match.Value.Trim();
        }

        // ── Main detection method ────────────────────────────────────────────
        public static HashSet<string> DetectEntities(string userQuestion)
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (pattern, entityType, _) in Rules)
            {
                var matches = pattern.Matches(userQuestion);
                foreach (Match match in matches)
                    detected.Add($"{entityType}:{ExtractValue(match)}");
            }

            return detected;
        }

        // ── Detect only entity types (without values) ────────────────────────
        public static HashSet<string> DetectEntityTypes(string userQuestion)
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var detected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (pattern, entityType, _) in Rules)
            {
                if (pattern.IsMatch(userQuestion))
                    detected.Add(entityType);
            }

            return detected;
        }

        // ── Detect with full detail ──────────────────────────────────────────
        public static List<DetectedEntity> DetectWithDetail(string userQuestion)
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                return new List<DetectedEntity>();

            var detected = new List<DetectedEntity>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (pattern, entityType, isScope) in Rules)
            {
                var matches = pattern.Matches(userQuestion);
                foreach (Match match in matches)
                {
                    var value = ExtractValue(match);
                    var key = $"{entityType}:{value}";
                    if (seen.Add(key))
                        detected.Add(new DetectedEntity
                        {
                            EntityType = entityType,
                            RawValue = value ?? string.Empty,
                            StartIndex = match.Index,
                            Length = match.Length,
                            IsScope = isScope
                        });
                }
            }

            return detected;
        }
    }

    // ── DetectedEntity model ─────────────────────────────────────────────────
    public class DetectedEntity
    {
        public string EntityType { get; set; } = string.Empty;
        public string RawValue { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public bool IsScope { get; set; }

        public override string ToString() => $"{EntityType}:{RawValue}";
    }
}