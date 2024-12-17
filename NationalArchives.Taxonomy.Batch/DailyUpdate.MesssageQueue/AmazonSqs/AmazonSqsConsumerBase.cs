﻿using Microsoft.Extensions.Logging;
using NationalArchives.ActiveMQ;
using NationalArchives.Taxonomy.Batch.DailyUpdate.MesssageQueue;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue
{
    internal abstract class AmazonSqsConsumerBase : ISourceIaidInputQueueConsumer, ISourceIaidInputQueueConsumerAdapter
    {
        protected ActiveMQ.Consumer<string> _msgConsumer;
        protected ILogger _logger;
        protected List<string> allIaidsReceived = new List<string>();
        protected CancellationToken _token;
        protected TaskCompletionSource<object> _tcsInit;

        private readonly IUpdateStagingQueueReceiver<string> _receiver;
        private readonly int _waitMilliseconds;

        public AmazonSqsConsumerBase(AmazonSqsParams queueParams, ILogger logger)
        {
            _receiver = new AmazonSqsReceiver<string>(queueParams);
            _logger = logger;
            _waitMilliseconds = queueParams.WaitMilliseconds;
        }

        public Task Init(CancellationToken token)
        {
            _tcsInit = new TaskCompletionSource<object>();

            _token = token;

            try
            {
                while (!_token.IsCancellationRequested)
                {
                    TaskAwaiter<List<string>> awaiter = _receiver.GetNextBatchOfResults(_logger, _waitMilliseconds).GetAwaiter();
                    List<string> listOfIaids = awaiter.GetResult();

                    if (listOfIaids?.Count > 0)
                    {
                        HandleTextMessage(listOfIaids).Wait();
                    }
                    else
                    {
                        // We didn't get anything back from the daily update queue. Wait 10 minutes before trying again.
                        Task.Delay(_waitMilliseconds).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_tcsInit.Task.IsFaulted)
                {
                    _tcsInit.SetException(ex); 
                }
            }

            return _tcsInit.Task;
        }

        public void Dispose()
        {

        }

        public int IaidCount
        {
            get => allIaidsReceived.Count;
        }

        protected abstract Task HandleTextMessage(IList<string> iaids);
    }
}