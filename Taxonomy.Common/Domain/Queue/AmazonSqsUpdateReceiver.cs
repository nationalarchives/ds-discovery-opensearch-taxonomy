using Amazon.Runtime;
using Amazon.SQS.Model;
using Amazon.SQS;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Amazon;
using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using System.Threading.Tasks;
using System.Threading;
using Amazon.Runtime.Internal.Util;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsUpdateReceiver : IUpdateStagingQueueReceiver, IDisposable
    {
        private const int FETCH_RETRY_COUNT = 5;
        private const string ROLE_SESSION_NAME = "Taxonomy_SQS_Update";

        private readonly AmazonSqsStagingQueueParams _qParams;    

        private AmazonSQSClient _client;

        public AmazonSqsUpdateReceiver(AmazonSqsStagingQueueParams qParams)
        {

            if(qParams == null || String.IsNullOrEmpty(qParams.QueueUrl))
            {
                throw new TaxonomyException(TaxonomyErrorType.SQS_EXCEPTION, "Invalid or missing queue parameters for Amazon SQS");
            }

            _qParams = qParams;

            try
            {
                RegionEndpoint region = RegionEndpoint.GetBySystemName(qParams.Region);

                if (!qParams.UseIntegratedSecurity)
                {
                    AWSCredentials credentials = null;

                    if (!String.IsNullOrEmpty(qParams.SessionToken))
                    {
                        credentials = new SessionAWSCredentials(awsAccessKeyId: qParams.AccessKey, awsSecretAccessKey: qParams.SecretKey, qParams.SessionToken);
                    }
                    else
                    {
                        credentials = new BasicAWSCredentials(accessKey: qParams.AccessKey, secretKey: qParams.SecretKey);
                    }

                    AWSCredentials aWSAssumeRoleCredentials = new AssumeRoleAWSCredentials(credentials, qParams.RoleArn, ROLE_SESSION_NAME);

                    _client = new AmazonSQSClient(aWSAssumeRoleCredentials, region);
                }
                else
                {
                    _client = new AmazonSQSClient(region);
                }

            }
            catch (Exception e)
            {
                Dispose();
                throw new TaxonomyException(TaxonomyErrorType.SQS_EXCEPTION, $"Error establishing a connection to Amazon SQS {qParams.QueueUrl}.", e);
            }
        }

        public async Task<List<IaidWithCategories>> GetNextBatchOfResults(Microsoft.Extensions.Logging.ILogger logger, int sqsRequestTimeoutSeconds)
        {
            List<IaidWithCategories> results = new List<IaidWithCategories>();
            List<DeleteMessageBatchRequestEntry> msgHandlesForDelete = new List<DeleteMessageBatchRequestEntry>();

            CancellationTokenSource fetchCancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(sqsRequestTimeoutSeconds));


            // Try long polling first. But sometimes this times out and brings back no results, even with the max 20 seconds wait time.
            // Therefore we have Short polling as a fallback.  This generally brings back fewer results, sometimes as few as 1 or 5 in testing.
            // See https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-short-and-long-polling.html#:~:text=The%20maximum%20long%20polling%20wait,t%20included%20in%20a%20response).
            var longPollingRequestParams = new ReceiveMessageRequest
            {
                QueueUrl = _qParams.QueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = TimeSpan.FromSeconds(Math.Min(sqsRequestTimeoutSeconds, 20)).Seconds  // 20 sconds the max for ReceiveMessageRequest but may want to use more for Cancel Token
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
                        List<IaidWithCategories> result = JsonConvert.DeserializeObject<List<IaidWithCategories>>(msg.Body);
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
                        CancellationTokenSource deleteCancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(sqsRequestTimeoutSeconds));
                        await _client.DeleteMessageBatchAsync(deleteRequest, deleteCancelSource.Token);
                    }
                    catch (TaskCanceledException tcex)
                    {
                        logger.LogWarning($"Request for taxonomy categorisation results from SQS queue {_qParams.QueueUrl} succeeded.  However the subsequent delete request for the message timed out after waiting {sqsRequestTimeoutSeconds} seconds");
                    } 
                }

                return results;
            }
            catch(TaskCanceledException tcex)
            {
                logger.LogError($"Request for taxonomy categorisation results from SQS queue {_qParams.QueueUrl} timed out after waiting {sqsRequestTimeoutSeconds} seconds");
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
