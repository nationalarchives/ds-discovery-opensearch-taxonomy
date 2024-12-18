using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsReceiver<T> : IUpdateStagingQueueReceiver<T>, IDisposable
    {
        private const int FETCH_RETRY_COUNT = 5;
        private const string ROLE_SESSION_NAME = "Taxonomy_SQS_Update";

        private readonly AmazonSqsParams _qParams;    

        private AmazonSQSClient _client;

        public AmazonSqsReceiver(AmazonSqsParams sqsParams)
        {

            if(sqsParams == null || String.IsNullOrEmpty(sqsParams.QueueUrl))
            {
                throw new TaxonomyException(TaxonomyErrorType.SQS_EXCEPTION, "Invalid or missing queue parameters for Amazon SQS");
            }

            _qParams = sqsParams;

            try
            {
                RegionEndpoint region = RegionEndpoint.GetBySystemName(sqsParams.Region);
                AWSCredentials credentials = sqsParams.GetCredentials(ROLE_SESSION_NAME);
                _client = new AmazonSQSClient(credentials, region);
            }
            catch (Exception e)
            {
                Dispose();
                throw new TaxonomyException(TaxonomyErrorType.SQS_EXCEPTION, $"Error establishing a connection to Amazon SQS {sqsParams.QueueUrl}.", e);
            }
        }

        public async Task<List<T>> GetNextBatchOfResults(Microsoft.Extensions.Logging.ILogger logger, int sqsRequestTimeoutMilliSeconds)
        {
            List<T> results = new List<T>();
            List<DeleteMessageBatchRequestEntry> msgHandlesForDelete = new List<DeleteMessageBatchRequestEntry>();

            CancellationTokenSource fetchCancelSource = new CancellationTokenSource(sqsRequestTimeoutMilliSeconds);

            // Try long polling first. But sometimes this times out and brings back no results, even with the max 20 seconds wait time.
            // Therefore we have Short polling as a fallback.  This generally brings back fewer results, sometimes as few as 1 or 5 in testing.
            // See https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-short-and-long-polling.html#:~:text=The%20maximum%20long%20polling%20wait,t%20included%20in%20a%20response).
            var longPollingRequestParams = new ReceiveMessageRequest
            {
                QueueUrl = _qParams.QueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = TimeSpan.FromSeconds(Math.Min(sqsRequestTimeoutMilliSeconds, 20000)).Seconds  // 20 seconds is the max for ReceiveMessageRequest but may want to use more for Cancel Token
            };

            var shortPollingRequestParams = new ReceiveMessageRequest
            {
                QueueUrl = _qParams.QueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 0 
            };

            ReceiveMessageResponse message = null;

            try
            {
                message = await _client.ReceiveMessageAsync(longPollingRequestParams, fetchCancelSource.Token);

                if (message == null)
                {
                    logger.LogWarning($"Request to SQS queue  {_qParams.QueueUrl} using Long Polling failed to retrieve any taxonomy classifcations.  Attempting Short Polling request");
                    message = await _client.ReceiveMessageAsync(shortPollingRequestParams, fetchCancelSource.Token);
                }
                else
                {
                    logger.LogInformation($"Long polling request to SQS queue brought back {message.Messages.Count} messages containing {message.Messages.SelectMany(m => m.MessageId).Count()} taxonomy results.");
                }

                if (message != null && message.Messages.Count > 0)
                {
                    foreach (Message msg in message?.Messages)
                    {
                        List<T> result = JsonConvert.DeserializeObject<List<T>>(msg.Body);
                        results.AddRange(result);
                        msgHandlesForDelete.Add(new DeleteMessageBatchRequestEntry() { Id = msg.MessageId, ReceiptHandle = msg.ReceiptHandle });
                    }
                }
                else
                {
                    logger.LogWarning($"Request to SQS queue {_qParams.QueueUrl} failed to retrieve any taxonomy classifcations.");
                }


                if (msgHandlesForDelete.Count > 0)
                {
                    var deleteRequest = new DeleteMessageBatchRequest()
                    {
                        QueueUrl = _qParams.QueueUrl,
                        Entries = msgHandlesForDelete
                    };

                    try
                    {
                        CancellationTokenSource deleteCancelSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(sqsRequestTimeoutMilliSeconds));
                        await _client.DeleteMessageBatchAsync(deleteRequest, deleteCancelSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        TimeSpan ts = TimeSpan.FromMilliseconds(sqsRequestTimeoutMilliSeconds);
                        logger.LogWarning($"Request for taxonomy categorisation results from SQS queue {_qParams.QueueUrl} succeeded.  However the subsequent delete request for the message timed out after waiting {ts.TotalSeconds} seconds.");
                    } 
                }

                return results;
            }
            catch(TaskCanceledException)
            {
                TimeSpan ts = TimeSpan.FromMilliseconds(sqsRequestTimeoutMilliSeconds);
                logger.LogError($"Request for taxonomy categorisation results from SQS queue {_qParams.QueueUrl} timed out after waiting for {ts.TotalSeconds} seconds.");
                return results;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
