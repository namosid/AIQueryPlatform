using AIQueryPlatform.SqlValidator.Models;
using AIQueryPlatform.SqlValidator.Services;

namespace AIQueryPlatform.UnitTest
{
    public class DBValidatorTest
    {
        [Fact]
        public void SyntaxValidator_Test()
        {
            var validator = new AIQueryPlatform.SqlValidator.Validators.SyntaxValidator();
            // Test case 1: Valid SQL
            var sql1 = @"WITH CurrentYear AS (
                SELECT AcademicYearID
                FROM AcademicYears
                WHERE IsCurrent = 1
            ),
            Grade2Section AS (
                SELECT DISTINCT s.SectionID, s.SectionName
                FROM Sections s
                INNER JOIN Classes c ON s.ClassID = c.ClassID
                WHERE c.ClassName = 'Grade 2'
            ),
            FeeData AS (
                SELECT fi.InvoiceNumber, fi.InvoiceDate, fi.NetAmount, fi.Status
                FROM FeeInvoices fi
                INNER JOIN StudentEnrollments se ON fi.StudentID = se.StudentID
                INNER JOIN Grade2Section gs ON se.SectionID = gs.SectionID
                INNER JOIN CurrentYear cy ON se.AcademicYearID = cy.AcademicYearID
            )
            SELECT DISTINCT gs.SectionID, gs.SectionName, fd.InvoiceNumber, fd.InvoiceDate, fd.NetAmount, fd.Status
            FROM FeeData fd
            INNER JOIN Grade2Section gs ON fd.SectionID = gs.SectionID;";
            var syntaxResult = validator.Validate(sql1);
            var pipeline = new PipelineResult { SQL = sql1, TurnNumber = 1 };
            if (syntaxResult.Errors.Any())
            {
                pipeline.FinalState = TurnState.SQLError;
                pipeline.FailedLayer = ValidationLayer.Syntax;
            }
            Assert.Empty(syntaxResult.Issues);
        }
    }
}