using System;

namespace NationalArchives.ActiveMQ
{
    public interface IConsumer<T> : IDisposable
    {
        /// <summary>
        /// Delegate used when a message is an object
        /// </summary>
        event MessageReceivedDelegate<T> OnMessageReceived;

        /// <summary>
        /// Delegate used when a message is a text
        /// </summary>
        event TextMessageReceivedDelegate OnTextMessageReceived;
    }
}