using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.Utils;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue
{
    internal class DeleteDocAmazonSqsMessageConsumer : AmazonSqsConsumerBase
    {
        //public DeleteDocActiveMqConsumer(MessageQueueParams msgQueueParams, ILogger<ActiveMqConsumerBase> logger) : base(msgQueueParams.BrokerUri, msgQueueParams.DeleteQueueName, logger)
        //{
        //}

        public DeleteDocAmazonSqsMessageConsumer(AmazonSqsParams queueParams, ILogger<DeleteDocAmazonSqsMessageConsumer> logger) : base(queueParams, logger)
        {
        }

        protected override async Task HandleTextMessage(IList<string> listOfIaids)
        {

            string summaryMessage = $"received Delete Document message from daily update queue, docReferences: {listOfIaids}, total = {listOfIaids.Count}";
            _logger.LogInformation(summaryMessage);
            Console.WriteLine(summaryMessage);

            TaxonomyDocumentMessageHolder deleteDocumentMessage = new TaxonomyDocumentMessageHolder(listOfIaids);

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
                _logger.LogWarning($"completed treatment for message from daily update queue with {deleteDocumentMessage.ListOfDocReferencesInError} errors");
                _logger.LogError($"DOCREFERENCES that raise an issue while deleting: {deleteDocumentMessage.ListOfDocReferencesInError}");
            }
            else
            {
                _logger.LogInformation($"completed treatment for message from daily update queue. {deleteDocumentMessage.ListOfDocReferences.Count} information assets processed.");
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
