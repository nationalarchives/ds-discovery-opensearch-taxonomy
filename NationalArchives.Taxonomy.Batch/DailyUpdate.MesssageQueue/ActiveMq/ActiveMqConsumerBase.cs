using Microsoft.Extensions.Logging;
using NationalArchives.ActiveMQ;
using NationalArchives.Taxonomy.Batch.DailyUpdate.MesssageQueue;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue
{
    internal abstract class ActiveMqConsumerBase : ISourceIaidInputQueueConsumer, ISourceIaidInputQueueConsumerAdapter
    {
        protected ActiveMQ.Consumer<string> _msgConsumer;
        protected ILogger<ActiveMqConsumerBase> _logger;
        protected List<string> allIaidsReceived = new List<string>();
        protected CancellationToken _token;
        protected TaskCompletionSource<object> _tcs;

        public ActiveMqConsumerBase(MessageQueueParams msgQueueParams, string queueName, ILogger<ActiveMqConsumerBase> logger)
        {

            if(!String.IsNullOrEmpty(msgQueueParams.Username) && !String.IsNullOrEmpty(msgQueueParams.Password))
            {
                _msgConsumer = new Consumer<string>(brokerUri: msgQueueParams.BrokerUri, queueName: queueName, userName: msgQueueParams.Username, password: msgQueueParams.Password);
            }
            else 
            {
                _msgConsumer = new Consumer<string>(brokerUri: msgQueueParams.BrokerUri, queueName: queueName);

            }


            _logger = logger;
        }

        public Task Init(CancellationToken token)
        {
            _tcs = new TaskCompletionSource<object>();

            _token = token;
            try
            {
                _msgConsumer.OnTextMessageReceivedWithId += this.HandleTextMessage;
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }

            return _tcs.Task;
        }

        public void Dispose()
        {
            _msgConsumer?.Dispose();
        }

        public int IaidCount
        {
            get => allIaidsReceived.Count;
        }

        protected abstract void HandleTextMessage(string messageId, string message);
    }
}
