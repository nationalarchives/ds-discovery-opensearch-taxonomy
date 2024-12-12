using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class AmazonSqsDirectUpdateSender : IUpdateStagingQueueSender, IDisposable
    {
        private readonly ConnectionFactory m_ConnectionFactory;
        private readonly IConnection m_Connection;
        private readonly ISession m_Session;
        private readonly IDestination m_destination;
        private readonly IMessageProducer m_Producer;

        private bool _addingCompleted;

        private readonly AmazonSqsStagingQueueParams _qParams;
        private readonly ILogger<IUpdateStagingQueueSender> _logger;

        private const string ROLE_SESSION_NAME = "Taxonomy_SQS_Update";

        /// <summary>
        /// Implementation of IUpdateStagingQueueSender where updates are sent
        /// directly to an ActiveMQ instance.
        /// </summary>
        /// <param name="qParams"></param>
        public AmazonSqsDirectUpdateSender(AmazonSqsStagingQueueParams qParams, ILogger<IUpdateStagingQueueSender> logger)
        {
            if(!qParams.PostUpdates)
            {
                return;
            }

            _qParams = qParams;
            _logger = logger;   
        }


        public Task<bool> Init(CancellationToken token, Action<int, int> updateQueueProgress)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Returns true for compatibility with other possible queue implemenations on the same interface</returns>
        public bool Enqueue(IaidWithCategories item, CancellationToken token)
        {
            if(token.IsCancellationRequested)
            {
                return false;
            }    

            if (item == null)
            {
                throw new TaxonomyException("No item supplied for interim queue update request!");
            }
            try
            {
                var itemAsList = new List<IaidWithCategories>() { item };

                AmazonSQSClient client;
                RegionEndpoint region = RegionEndpoint.GetBySystemName(_qParams.Region);

                if (!_qParams.UseIntegratedSecurity)
                {
                    AWSCredentials credentials = null;

                    if (!String.IsNullOrEmpty(_qParams.SessionToken))
                    {
                        credentials = new SessionAWSCredentials(awsAccessKeyId: _qParams.AccessKey, awsSecretAccessKey: _qParams.SecretKey, _qParams.SessionToken);
                    }
                    else
                    {
                        credentials = new BasicAWSCredentials(accessKey: _qParams.AccessKey, secretKey: _qParams.SecretKey);
                    }

                    AWSCredentials aWSAssumeRoleCredentials = new AssumeRoleAWSCredentials(credentials, _qParams.RoleArn, ROLE_SESSION_NAME);

                    client = new AmazonSQSClient(aWSAssumeRoleCredentials, region);
                }
                else
                {
                    client = new AmazonSQSClient(region);
                }

                var request = new SendMessageRequest()
                {
                    MessageBody = JsonConvert.SerializeObject(itemAsList),
                    QueueUrl = _qParams.QueueUrl,
                };

                var awaiter = client.SendMessageAsync(request).GetAwaiter();
                var result = awaiter.GetResult();

                return true;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public bool IsAddingCompleted
        {
            get => _addingCompleted;
        }

        public IReadOnlyCollection<string> QueueUpdateErrors => throw new NotImplementedException();

        public void Dispose()
        {
            m_Producer?.Dispose();
            m_Session?.Dispose();
            m_Connection?.Dispose();
        }


        public void CompleteAdding()
        {
            _addingCompleted = true;
        }
    }
}
