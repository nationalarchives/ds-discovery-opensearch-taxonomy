using Apache.NMS;
using Apache.NMS.ActiveMQ;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class ActiveMqUpdateReceiver : IUpdateStagingQueueReceiver, IDisposable
    {
        private const int FETCH_RETRY_COUNT = 5;

        private readonly ConnectionFactory m_ConnectionFactory;
        private readonly IConnection m_Connection;
        private readonly ISession m_Session;
        private readonly IDestination m_destination;
        private readonly IMessageConsumer m_Consumer;

        public ActiveMqUpdateReceiver(UpdateStagingQueueParams qParams)
        {

            if(qParams == null || String.IsNullOrEmpty(qParams.QueueName) || String.IsNullOrEmpty(qParams.Uri))
            {
                throw new TaxonomyException(TaxonomyErrorType.JMS_EXCEPTION, "Invalid or missing queue parameters for Active MQ");
            }

            try
            {
                m_ConnectionFactory = new ConnectionFactory(qParams.Uri);
                if (!String.IsNullOrWhiteSpace(qParams.UserName) && !String.IsNullOrWhiteSpace(qParams.Password))
                {
                    m_Connection = m_ConnectionFactory.CreateConnection(qParams.UserName, qParams.Password);
                }
                else
                { 
                    m_Connection = m_ConnectionFactory.CreateConnection(); 
                }
                m_Connection.Start();
                m_Session = m_Connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
                m_destination = m_Session.GetQueue(qParams.QueueName);
                m_Consumer = m_Session.CreateConsumer(m_destination);

            }
            catch (Exception e)
            {
                Dispose();
                throw new TaxonomyException(TaxonomyErrorType.JMS_EXCEPTION, $"Error establishing a connection to ActiveMQ {qParams.QueueName}, at {qParams.Uri}", e);
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
            IMessage nextItem;
            IBytesMessage nextBytesMessage = null;
            int attempts = 0;

            do
            {
                nextItem = m_Consumer.ReceiveNoWait();
                if (nextItem != null)
                {
                    nextBytesMessage = nextItem as IBytesMessage;
                    
                }
                else
                {
                    attempts++;
                }
            } while (nextItem == null && attempts <= FETCH_RETRY_COUNT);


            if (nextBytesMessage != null)
            {
                byte[] bytes = nextBytesMessage.Content;
                List<IaidWithCategories> nextBatchFromInterimQueue = IaidWithCategoriesSerialiser.IdxMessageToListOfIaidsWithCategories(bytes);
                return nextBatchFromInterimQueue;
            }
            else
            {
                return null;
            }
        }
    }
}
