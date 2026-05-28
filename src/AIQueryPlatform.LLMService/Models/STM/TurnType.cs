using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Models.STM
{
    public enum TurnType
    {
        FreshQuery,         // new unrelated question
        ClarificationAsked, // system asked for more info
        ClarificationGiven, // user answered clarification
        RefinedQuery,       // query refined from context
        FollowUp,           // refers to previous turn
        SQLError,
        SQLErrorResolved
    }
}
