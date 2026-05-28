using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIQueryPlatform.LLMServiceOperator.Services.STM
{
    public class SessionManager
    {
        private static readonly SessionManager _instance = new();
        public static SessionManager Instance => _instance;

        private readonly Dictionary<string, SessionContext> _sessions = new();

        private SessionManager() { }

        public SessionContext GetOrCreate(string conversationID)
        {
            if (_sessions.TryGetValue(conversationID, out var existing))
                return existing;

            var session = new SessionContext();
            _sessions[conversationID] = session;
            return session;
        }
    }
}
