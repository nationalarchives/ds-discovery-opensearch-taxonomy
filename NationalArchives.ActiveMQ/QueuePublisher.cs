using Apache.NMS;
using Apache.NMS.ActiveMQ;
using System;

namespace NationalArchives.ActiveMQ
{
    public class QueuePublisher : IPublisher
    {
        private readonly ConnectionFactory m_ConnectionFactory;
        private readonly IConnection m_Connection;
        private readonly ISession m_Session;
        private readonly IMessageProducer m_Producer;
        private readonly IDestination m_Destination;
        private bool m_IsDisposed = false;

        public QueuePublisher(string brokerUri, string queueName)
        {
            try
            {
                m_ConnectionFactory = new ConnectionFactory(brokerUri);
                m_Connection = m_ConnectionFactory.CreateConnection();
                m_Connection.Start();
                m_Session = m_Connection.CreateSession();
                m_Destination = m_Session.GetQueue(queueName);
                m_Producer = m_Session.CreateProducer(m_Destination);
                m_Producer.DeliveryMode = MsgDeliveryMode.Persistent;
            }
            catch (NMSConnectionException) { throw; }
        }

        public QueuePublisher(string brokerUri, string queueName, string userName, string password)
        {
            foreach(string s in new string[] { brokerUri, queueName, userName, password })
            {
                if(String.IsNullOrEmpty(s))
                {
                    throw new ArgumentException("Queue Publisher requires Broker Uri, Queue Name, User Name and Password.");
                }
            }
            
            try
            {
                m_ConnectionFactory = new ConnectionFactory(brokerUri);
                m_Connection = m_ConnectionFactory.CreateConnection(userName, password);
                m_Connection.Start();
                m_Session = m_Connection.CreateSession();
                m_Destination = m_Session.GetQueue(queueName);
                m_Producer = m_Session.CreateProducer(m_Destination);
                m_Producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
            }
            catch (NMSConnectionException) { throw; }
        }

        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Message text</param>
        public void SendMessage(string message)
        {
            if (!m_IsDisposed)
            {
                try
                {
                    var objectMessage = m_Producer.CreateTextMessage(message);
                    m_Producer.Send(objectMessage);
                }
                catch (NMSConnectionException) { throw; }
                catch (NMSSecurityException) { throw; }
                catch (NMSException) { throw; }
            }
            else
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        /// <summary>
        /// Send an object message
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="messageObject">Object message</param>
        public void SendMessage<T>(T messageObject)
        {
            if (!m_IsDisposed)
            {
                try
                {
                    var objectMessage = m_Producer.CreateObjectMessage(messageObject);
                    m_Producer.Send(objectMessage);
                }
                catch (NMSConnectionException) { throw; }
                catch (NMSSecurityException) { throw; }
                catch (NMSException) { throw; }
            }
            else
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }

        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                // Clean up
                m_Producer.Close();
                m_Session.Close();
                m_Connection.Close();

                m_Producer.Dispose();
                m_Session.Dispose();
                m_Connection.Dispose();
                m_IsDisposed = true;
            }
        }
    }
}