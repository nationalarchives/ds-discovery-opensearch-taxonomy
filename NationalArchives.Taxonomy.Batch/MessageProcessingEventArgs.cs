using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Batch
{
    internal sealed class MessageProcessingEventArgs : EventArgs
    {
        public MessageProcessingEventArgs()
        {
                
        }

        public MessageProcessingEventArgs(string msg)
        {
            Message = msg;
        }

        public string Message { get; set; }
    }

    internal enum MessageProcessingEventType
    {
        FATAL_EXCEPTION,
        PROCESSING_COMPLETE
    }
}
