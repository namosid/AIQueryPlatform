using AIQueryPlatform.LLMServiceOperator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services
{
    public class OutputRuleEngine
    {
        public readonly List<OutputRule> _rules;

        public OutputRuleEngine()
        {
            _rules = BuildRules();
        }

        public string BuildOutputRules(string question)
        {
            question = question.ToLower();

            var selectedRules = new List<string>();

            foreach (var rule in _rules)
            {
                if (question.Contains(rule.EntityName.ToLower()))
                {
                    selectedRules.Add(CreateRuleText(rule));
                }
            }

            // Additional semantic mappings

            if (question.Contains("student"))
            {
                selectedRules.Add(
                    CreateRuleText(
                        _rules.First(x => x.EntityName == "Students")));
            }

            if (question.Contains("teacher"))
            {
                selectedRules.Add(
                    CreateRuleText(
                        _rules.First(x => x.EntityName == "Staff")));
            }

            if (question.Contains("exam") || question.Contains("mark"))
            {
                selectedRules.Add(
                    CreateRuleText(
                        _rules.First(x => x.EntityName == "ExamResults")));
            }

            return string.Join(
                Environment.NewLine,
                selectedRules.Distinct());
        }

        private string CreateRuleText(OutputRule rule)
        {
            var mandatory =
                string.Join(", ", rule.MandatoryColumns);

            var optional =
                string.Join(", ", rule.OptionalColumns);

            return
    $"""
OUTPUT RULE:
If query includes {rule.EntityName},
ALWAYS include:
{mandatory}

OPTIONAL:
{optional}

{rule.Instruction}
""";
        }

        private List<OutputRule> BuildRules()
        {
            return new List<OutputRule>
            {
                new OutputRule
                {
                    EntityName = "Students",
                    MandatoryColumns = new List<string>
                    {
                        "AdmissionNumber",
                        "FirstName",
                        "LastName",
                        "ClassName",
                        "SectionName",
                        "RollNumber"
                    },

                    OptionalColumns = new List<string>
                    {
                         "DateOfBirth",
                    },
                    RequiredEntities = new List<string>(),
                    Instruction =
                        "Prefer readable student information instead of IDs only."
                },

                new OutputRule
                {
                    EntityName = "Staff",

                    MandatoryColumns = new List<string>
                    {
                        "StaffID",
                        "FirstName",
                        "LastName"
                    },

                    OptionalColumns = new List<string>
                    {
                        "DepartmentName",
                        "Designation"
                    },
                    RequiredEntities = new List<string>(),
                    Instruction =
                        "Prefer readable staff information instead of IDs only."
                },

                new OutputRule
                {
                    EntityName = "Teacher",

                    MandatoryColumns = new List<string>
                    {
                        "StaffID",
                        "FirstName",
                        "LastName"
                    },

                    OptionalColumns = new List<string>
                    {
                        "DepartmentName",
                        "Designation"
                    },
                     RequiredEntities = new List<string>(),
                    Instruction =
                        "Prefer readable staff information instead of IDs only."
                },

                new OutputRule
                {
                    EntityName = "Subjects",

                    MandatoryColumns = new List<string>
                    {
                        "SubjectID",
                        "SubjectName"
                    },

                    OptionalColumns = new List<string>
                    {
                        "SubjectCode"
                    },
                     RequiredEntities = new List<string>(),
                    Instruction =
                        "Include subject descriptive information."
                },

                new OutputRule
                {
                    EntityName = "Classes",

                    MandatoryColumns = new List<string>
                    {
                        "ClassID",
                        "ClassName"
                    },
                    RequiredEntities = new List<string>(),
                    Instruction =
                        "Prefer class names over IDs."
                },

                new OutputRule
                {
                    EntityName = "Sections",

                    MandatoryColumns = new List<string>
                    {
                        "SectionID",
                        "SectionName"
                    },
                    RequiredEntities = new List<string>(),
                    Instruction =
                        "Prefer section names over IDs."
                },


                 new OutputRule
                    {
                        EntityName       = "Fees",
                        MandatoryColumns = new List<string>
                        {
                            "FeeAmount",
                            "DueDate",
                            "Status"
                        },
                        OptionalColumns  = new List<string>
                        {
                            "FeeCategoryName",
                            "PaidDate"
                        },
                        RequiredEntities = new List<string>
                        {
                            "Students"  // ← fees always need student identity
                        }
                    },

                  new OutputRule
                    {
                        EntityName       = "Mark",
                        MandatoryColumns = new List<string>
                        {
                            "MarksObtained",
                            "Grade",
                            "ExamTypeName",
                            "AcademicYear"
                        },
                        OptionalColumns  = new List<string>
                        {
                            "Remarks",
                            "HallNumber"
                        },
                        RequiredEntities = new List<string>
                        {
                            "Students"  // ← fees always need student identity
                        }
                    },
                  new OutputRule
                    {
                        EntityName       = "ExamResults",
                        MandatoryColumns = new List<string>
                        {
                            "MarksObtained",
                            "Grade",
                            "ExamTypeName",
                            "YearLabel"
                        },
                        OptionalColumns  = new List<string>
                        {
                            "Remarks",
                            "HallNumber"
                        },
                        RequiredEntities = new List<string>
                        {
                            "Students",
                            "AcademicYears"// ← fees always need student identity
                        }
                    }
            };
        }
    }
}
