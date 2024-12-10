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

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsUpdateReceiver : IUpdateStagingQueueReceiver, IDisposable
    {
        private const int FETCH_RETRY_COUNT = 5;
        private const string ROLE_SESSION_NAME = "Taxonomy_SQS_Update";

        private readonly ConnectionFactory m_ConnectionFactory;
        private readonly IConnection m_Connection;
        private readonly ISession m_Session;
        private readonly IDestination m_destination;
        private readonly IMessageConsumer m_Consumer;

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

        public IList<IaidWithCategories> DequeueIaidsWithCategories(int numberToFetch)
        {
            throw new NotImplementedException();
        }

        public IaidWithCategories DeQueueNextIaidWithCategories()
        {

            IMessage nextItem;
            int attempts = 0;

            do
            {
                nextItem = m_Consumer.ReceiveNoWait();
                attempts++;
            } while (nextItem == null && attempts <= FETCH_RETRY_COUNT);

            ITextMessage msg = nextItem as ITextMessage;

            if(msg != null)
            {
                IaidWithCategories iaidWithCategories = JsonConvert.DeserializeObject<IaidWithCategories>(msg.Text);
                return iaidWithCategories;
            }
            else
            {
                return null;
            }
        }

        public void Dispose()
        {
            m_Consumer?.Dispose();
            m_Session?.Dispose();
            m_Connection?.Dispose();
        }

        public List<IaidWithCategories> DeQueueNextListOfIaidsWithCategories()
        {
            var requestParams = new ReceiveMessageRequest
            {
                QueueUrl = _qParams.QueueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = TimeSpan.FromSeconds(10).Seconds,
            };

            ReceiveMessageResponse message = _client.ReceiveMessageAsync(requestParams).Result;
            if (message.Messages.Count == 1)
            {
                List<IaidWithCategories> result = JsonConvert.DeserializeObject<List<IaidWithCategories>>(message.Messages[0].Body);
                return result;
            }
            else
            {
                throw new TaxonomyException("Unexpected message count");
            }
        }

        public async IAsyncEnumerable<List<IaidWithCategories>> IterateResults()
        {
            var requestParams = new ReceiveMessageRequest
            {
                QueueUrl = _qParams.QueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = TimeSpan.FromSeconds(10).Seconds,
            };

            ReceiveMessageResponse message = null;

            try
            {
                message = await _client.ReceiveMessageAsync(requestParams);
            }
            catch (Exception ex)
            {
                throw;
            }

            foreach (Message msg in message.Messages)
            {
                List<IaidWithCategories> result = JsonConvert.DeserializeObject<List<IaidWithCategories>>(msg.Body);
                await _client.DeleteMessageAsync(_qParams.QueueUrl, msg.ReceiptHandle);
                yield return result;
            }
            Console.WriteLine("Done");
        }
    }
}
