using System;
using System.Collections.Generic;
using System.Text;

namespace clock_client
{
    public class TopIndentWithPredictionResponse
    {
        public string TopIntent { get; set; } = string.Empty;
        public dynamic ConversationPrediction { get; set; }
    }
}
