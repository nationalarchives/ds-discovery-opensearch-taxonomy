using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue
{
    internal class MessageQueueParams
    {
        public string BrokerUri { get; set; }
        public string UpdateQueueName { get; set; }
        public string DeleteQueueName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

    }
}
