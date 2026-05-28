using AIQueryPlatform.LLMServiceOperator.Models.STM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.STM
{
    public static class QueryTypeDetector
    {
        public static QueryType Detect(string refinedQuery)
        {
            if (Regex.IsMatch(refinedQuery,
                @"\b(personal|info|information|profile|detail|name|dob|address)\b",
                RegexOptions.IgnoreCase))
                return QueryType.PersonalInfo;

            if (Regex.IsMatch(refinedQuery,
                @"\b(attendance|present|absent|leaves)\b",
                RegexOptions.IgnoreCase))
                return QueryType.Attendance;

            if (Regex.IsMatch(refinedQuery,
                @"\b(marks|result|score|grade|exam|performance)\b",
                RegexOptions.IgnoreCase))
                return QueryType.ExamResult;

            if (Regex.IsMatch(refinedQuery,
                @"\b(fee|payment|dues|invoice|challan)\b",
                RegexOptions.IgnoreCase))
                return QueryType.FeePayment;

            if (Regex.IsMatch(refinedQuery,
                @"\b(class|section|grade|report)\b",
                RegexOptions.IgnoreCase))
                return QueryType.ClassReport;

            if (Regex.IsMatch(refinedQuery,
                @"\b(staff|teacher)\b",
                RegexOptions.IgnoreCase))
                return QueryType.StaffInfo;

            return QueryType.Unknown;
        }
    }
}
