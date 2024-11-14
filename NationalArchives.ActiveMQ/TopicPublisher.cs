using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System;

namespace NationalArchives.ActiveMQ
{
    public class TopicPublisher : IPublisher
    {
        private readonly ConnectionFactory m_ConnectionFactory;
        private readonly IConnection m_Connection;
        private readonly ISession m_Session;
        private readonly IMessageProducer m_Producer;
        private readonly ActiveMQTopic m_Topic;
        private bool m_IsDisposed = false;

        public TopicPublisher(string brokerUri, string topicName)
        {
            try
            {
                m_ConnectionFactory = new ConnectionFactory(brokerUri);
                m_Connection = m_ConnectionFactory.CreateConnection();
                m_Connection.Start();
                m_Session = m_Connection.CreateSession();
                m_Topic = new ActiveMQTopic(topicName);
                m_Producer = m_Session.CreateProducer(m_Topic);
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
            { // Clean up
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