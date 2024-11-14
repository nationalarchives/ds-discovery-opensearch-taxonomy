using System;
namespace NationalArchives.ActiveMQ
{
    public interface IPublisher : IDisposable
    {
        /// <summary>
        /// Send a text message
        /// </summary>
        /// <param name="message">Message text</param>
        void SendMessage(string message);

        /// <summary>
        /// Send an object message
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="messageObject">Object message</param>
        void SendMessage<T>(T messageObject);
    }
}