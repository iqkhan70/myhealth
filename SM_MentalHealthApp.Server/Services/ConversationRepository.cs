using System;
using System.Collections.Generic;

namespace SM_MentalHealthApp.Server.Services
{
    public class ConversationRepository
    {
        private readonly Dictionary<Guid, string> _conversations = new();

        public string GetLastResponseId(Guid conversationId)
        {
            return _conversations.TryGetValue(conversationId, out var responseId) ? responseId : null;
        }

        public void SetLastResponseId(Guid conversationId, string responseId)
        {
            _conversations[conversationId] = responseId;
        }
    }
}
