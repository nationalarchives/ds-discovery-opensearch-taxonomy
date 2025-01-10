using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.DataObjects.OpenSearch
{
    public sealed class OpenSearchUpdateEventArgs : EventArgs
    {
        public OpenSearchUpdateEventArgs()
        {
                
        }

        public OpenSearchUpdateEventArgs(string msg, OpenSearchUpdateEventType eventType )
        {
            Message = msg;
            EventType = eventType;
        }

        public string Message { get;}
        public OpenSearchUpdateEventType EventType { get;}
    }

    public enum OpenSearchUpdateEventType
    {
        FATAL_EXCEPTION,
        PROCESSING_COMPLETE
    }
}
