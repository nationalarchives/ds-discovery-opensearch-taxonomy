using System;
using System.Collections.Generic;
using System.Text;

namespace NationalArchives.Taxonomy.Common.DataObjects.Elastic
{
    public sealed class ElasticUpdateEventArgs : EventArgs
    {
        public ElasticUpdateEventArgs()
        {
                
        }

        public ElasticUpdateEventArgs(string msg, ElasticUpdateEventType eventType )
        {
            Message = msg;
            EventType = eventType;
        }

        public string Message { get;}
        public ElasticUpdateEventType EventType { get;}
    }

    public enum ElasticUpdateEventType
    {
        FATAL_EXCEPTION,
        PROCESSING_COMPLETE
    }
}
