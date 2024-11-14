using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System;

namespace NationalArchives.ActiveMQ
{
    ///// <summary>
    ///// Delegate used when a message is an object
    ///// </summary>
    public delegate void MessageReceivedDelegate<T>(T message);

    ///// <summary>
    ///// Delegate used when a message is a text
    ///// </summary>
    public delegate void TextMessageReceivedDelegate(string message);
    public delegate void TextMessageReceivedWithIdDelegate(string messageId, string message);

    public class Consumer<T> : IConsumer<T>
    {
        private readonly ConnectionFactory m_ConnectionFactory;
        private readonly IConnection m_Connection;
        private readonly ISession m_Session;
        private readonly IMessageConsumer m_Consumer;
        private readonly IDestination m_destination;
        private readonly ActiveMQTopic m_Topic;
        private bool m_IsDisposed = false;

        public event MessageReceivedDelegate<T> OnMessageReceived;

        public event TextMessageReceivedDelegate OnTextMessageReceived;
        public event TextMessageReceivedWithIdDelegate OnTextMessageReceivedWithId;

        /// <summary>
        /// Initialises consumer for the specific queue
        /// </summary>
        /// <param name="brokerUri">Broker URI</param>
        /// <param name="queueName">Name of the queue</param>
        public Consumer(string brokerUri, string queueName)
        {
            try
            {
                m_ConnectionFactory = new ConnectionFactory(brokerUri);
                m_Connection = m_ConnectionFactory.CreateConnection();
                m_Connection.Start();
                m_Session = m_Connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
                m_destination = m_Session.GetQueue(queueName);
                m_Consumer = m_Session.CreateConsumer(m_destination);
                m_Consumer.Listener += new MessageListener(OnMessage);
            }
            catch (NMSConnectionException) { throw; }
        }

        /// <summary>
        /// Initialises topic consumer
        /// </summary>
        /// <param name="brokerUri">Uri of the messaging service</param>
        /// <param name="topicName">Topic name</param>
        /// <param name="clientId">Client ID for the connection</param>
        /// <param name="consumerId">Consumer ID</param>
        public Consumer(string brokerUri, string topicName, string clientId, string consumerId)
        {
            try
            {
                m_ConnectionFactory = new ConnectionFactory(brokerUri);
                m_Connection = m_ConnectionFactory.CreateConnection();
                m_Connection.ClientId = clientId;
                m_Connection.Start();
                m_Session = m_Connection.CreateSession();
                m_Topic = new ActiveMQTopic(topicName);
                //keeps the message until the consumer is restarted
                m_Consumer = m_Session.CreateDurableConsumer(m_Topic, consumerId, null, false);
                m_Consumer.Listener += new MessageListener(OnMessage);
            }
            catch (NMSConnectionException) { throw; }
        }

        public Consumer(string brokerUri, string queueName, string userName, string password, string clientId = null, string consumerId = null)
        {
            foreach (string s in new string[] { brokerUri, queueName, userName, password })
            {
                if (String.IsNullOrEmpty(s))
                {
                    throw new ArgumentException("Queue Publisher requires Broker Uri, Queue Name, User Name and Password.");
                }
            }

            try
            {
                m_ConnectionFactory = new ConnectionFactory(brokerUri);
                m_Connection = m_ConnectionFactory.CreateConnection(userName, password);
                m_Connection.Start();

                if(!string.IsNullOrWhiteSpace(clientId))
                {
                    m_Connection.ClientId = clientId;
                }

                m_Session = m_Connection.CreateSession(AcknowledgementMode.AutoAcknowledge);
                m_destination = m_Session.GetQueue(queueName);

                m_Consumer = m_Session.CreateConsumer(m_destination);

                if (!string.IsNullOrWhiteSpace(consumerId))
                {
                    //keeps the message until the consumer is restarted
                    m_Consumer = m_Session.CreateDurableConsumer(m_Topic, consumerId, null, false);
                }
                m_Consumer.Listener += new MessageListener(OnMessage);
            }
            catch (NMSConnectionException) { throw; }
        }

        public string ReceiveNextAsString()
        {
            IMessage message = this.m_Consumer.Receive();
            return (!(message is ITextMessage) ? message.ToString() : ((ITextMessage)message).Text);
        }



        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_Consumer.Close();
                m_Session.Close();
                m_Connection.Close();

                m_Consumer.Dispose();
                m_Session.Dispose();
                m_Connection.Dispose();
                m_IsDisposed = true;
            }
        }

        private void OnMessage(IMessage message)
        {
            try
            {
                if ((message is IObjectMessage) && (this.OnMessageReceived != null))
                {
                    IObjectMessage message2 = message as IObjectMessage;
                    this.OnMessageReceived((T)message2.Body);
                }
                else if (message is ITextMessage)
                {
                    ITextMessage message3 = message as ITextMessage;
                    if (this.OnTextMessageReceived != null)
                    {
                        this.OnTextMessageReceived(message3.Text);
                    }
                    if (this.OnTextMessageReceivedWithId != null)
                    {
                        this.OnTextMessageReceivedWithId(message3.NMSMessageId, message3.Text);
                    }
                }
            }
            catch (NMSConnectionException) { throw; }
            catch (NMSSecurityException) { throw; }
            catch (NMSException) { throw; }
        }
    }
}