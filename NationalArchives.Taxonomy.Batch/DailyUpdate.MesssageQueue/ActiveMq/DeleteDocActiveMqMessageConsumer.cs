using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.Utils;
using NationalArchives.Taxonomy.Common;
using System;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue
{
    internal class DeleteDocActiveMqConsumer : ActiveMqConsumerBase
    {
        //public DeleteDocActiveMqConsumer(MessageQueueParams msgQueueParams, ILogger<ActiveMqConsumerBase> logger) : base(msgQueueParams.BrokerUri, msgQueueParams.DeleteQueueName, logger)
        //{
        //}

        public DeleteDocActiveMqConsumer(MessageQueueParams msgQueueParams, ILogger<ActiveMqConsumerBase> logger) : base(msgQueueParams, msgQueueParams.DeleteQueueName, logger)
        {
        }

        protected override void HandleTextMessage(string messageId, string message)
        {
            IList<string> iaidsInMessage = message.GetListOfDocReferencesFromMessage();

            string summaryMessage = $"received Delete Document message: {messageId}, docReferences: {iaidsInMessage}, total = {iaidsInMessage.Count}";
            _logger.LogInformation(summaryMessage);
            Console.WriteLine(summaryMessage);

            TaxonomyDocumentMessageHolder deleteDocumentMessage = new TaxonomyDocumentMessageHolder(messageId, iaidsInMessage);

            foreach (string iaid in deleteDocumentMessage.ListOfDocReferences)
            {
                try
                {
                    RemoveDocumentFromMongoByDocReference(iaid);
                    allIaidsReceived.Add(iaid);
                }
                catch (TaxonomyException e)
                {
                    deleteDocumentMessage.AddDocReferenceInError(iaid);
                    _logger.LogError($"Error processing deletion request on iaid {iaid} from message queue.", e);
                }
                catch (Exception e)
                {
                    deleteDocumentMessage.AddDocReferenceInError(iaid);
                    TaxonomyException te = new TaxonomyException(TaxonomyErrorType.JMS_EXCEPTION, $"Error processing iaid {iaid} from delete message queue.", e);
                    _logger.LogError($"Error processing dlete request for iaid {iaid}", e);
                }
            }

            if (deleteDocumentMessage.HasProcessingErrors)
            {
                _logger.LogWarning($"completed treatment for message: {deleteDocumentMessage.MessageId} with {deleteDocumentMessage.ListOfDocReferencesInError} errors");
                _logger.LogError($"DOCREFERENCES that raise an issue while deleting: {deleteDocumentMessage.ListOfDocReferencesInError}");
            }
            else
            {
                _logger.LogInformation($"completed treatment for message: {messageId}. {deleteDocumentMessage.ListOfDocReferences.Count} information assets processed.");
            }
        }

        private void RemoveDocumentFromMongoByDocReference(String docReference)
        {

            // TODO: Remove document from Mongo databases as required.  Possibly includes a local instance.
            // Do we have any existing service /code for removing a document from Mongo e.g. back office?
            // Possibly use the Delete API which apparently can delete a Mongo document.
            _logger.LogInformation($"A Taxonomy Delete request was received for iaid: {docReference}. However no deletion code is currently configured.");

            // informationAssetViewMongoRepository.delete(docReference);
            // iaViewUpdateRepository.findAndRemoveByDocReference(docReference);
        }

    }
}
