using Apache.NMS;
using Apache.NMS.ActiveMQ;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Common.Domain.Queue
{
    public class ActiveMqDirectUpdateSender : IUpdateStagingQueueSender, IDisposable
    {
        private readonly ConnectionFactory m_ConnectionFactory;
        private readonly IConnection m_Connection;
        private readonly ISession m_Session;
        private readonly IDestination m_destination;
        private readonly IMessageProducer m_Producer;

        private bool _addingCompleted;

        /// <summary>
        /// Implementation of IUpdateStagingQueueSender where updates are sent
        /// directly to an ActiveMQ instance.
        /// </summary>
        /// <param name="qParams"></param>
        public ActiveMqDirectUpdateSender(UpdateStagingQueueParams qParams)
        {
            if(!qParams.PostUpdates)
            {
                return;
            }

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
            m_Producer = m_Session.CreateProducer(m_destination);
        }


        public Task<bool> Init(CancellationToken token, Action<int, int> updateQueueProgress)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Returns true for compatibility with other possible queue implemnenations on the same interface</returns>
        public bool Enqueue(IaidWithCategories item, CancellationToken token)
        {
            if(m_Producer == null)
            {
                return false;
            }

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
                //string itemString = JsonConvert.SerializeObject(item);
                //var textMessage = m_Producer.CreateTextMessage(itemString);
                // m_Producer.Send(textMessage);

                byte[] serialisedResult = item.ToByteArray();
                var bytesMessage = m_Producer.CreateBytesMessage(serialisedResult);
                m_Producer.Send(bytesMessage);


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
