using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.Utils;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue
{
    internal sealed class CategoriseDocActiveMqConsumer : ActiveMqConsumerBase
    {
        private readonly ICategoriserService<CategorisationResult> _categoriserService;
        //TODO: Event to notify service when processing from queue complete.

        //public CategoriseDocActiveMqConsumer(ICategoriserService<CategorisationResult> categoriserService, 
        //    MessageQueueParams inputMsgQueueParams, ILogger<ActiveMqConsumerBase> logger) : base(inputMsgQueueParams.BrokerUri, inputMsgQueueParams.UpdateQueueName, logger)
        //{
        //    _categoriserService = categoriserService;
        //}

        public CategoriseDocActiveMqConsumer(ICategoriserService<CategorisationResult> categoriserService,
            MessageQueueParams inputMsgQueueParams, ILogger<ActiveMqConsumerBase> logger) : base(inputMsgQueueParams, inputMsgQueueParams.UpdateQueueName, logger)
        {
            _categoriserService = categoriserService;
        }

        protected override void HandleTextMessage(string messageId, string message)
        {
            try
            {
                if (!_token.IsCancellationRequested)
                {
                    IList<string> iaidsInMessage = message.GetListOfDocReferencesFromMessage();

                    if (iaidsInMessage == null || iaidsInMessage.Count == 0)
                    {
                        _tcs.SetResult(null);
                    }
                    else
                    {
                        string summaryMessage = $"received Categorise Document message: {messageId}, docReferences: {String.Join(',', iaidsInMessage)}, total = {iaidsInMessage.Count}";
                        _logger.LogInformation(summaryMessage);
                        Console.WriteLine(summaryMessage);

                        var categoriseDocumentMessage = new TaxonomyDocumentMessageHolder(messageId, iaidsInMessage);

                        foreach (string iaid in categoriseDocumentMessage.ListOfDocReferences)
                        {
                            try
                            {
                                var awaiter = _categoriserService.CategoriseSingle(iaid).GetAwaiter();
                                IList<CategorisationResult> results = awaiter.GetResult();


                                int numberOfCategoriesMatched = results.Count;
                                _logger.LogInformation($"  - Categorised {iaid}, {numberOfCategoriesMatched} " + (numberOfCategoriesMatched == 1 ? "category" : "categories") + $" found: " + String.Join(';', results));

                                allIaidsReceived.Add(iaid);
                            }
                            catch (TaxonomyException e)
                            {
                                categoriseDocumentMessage.AddDocReferenceInError(iaid);
                                _logger.LogError($"error processing iaid {iaid} from message queue.", e);
                            }
                            catch (Exception e)
                            {
                                categoriseDocumentMessage.AddDocReferenceInError(iaid);
                                TaxonomyException te = new TaxonomyException(TaxonomyErrorType.JMS_EXCEPTION, $"Error processing iaid {iaid} from message queue.", e);
                                throw te;
                            }
                        }

                        if (categoriseDocumentMessage.HasProcessingErrors)
                        {
                            _logger.LogWarning($"completed treatment for message: {categoriseDocumentMessage.MessageId} with {categoriseDocumentMessage.ListOfDocReferencesInError} errors");
                            _logger.LogError($"DOCREFERENCES THAT COULD NOT BE CATEGORISED: {categoriseDocumentMessage.ListOfDocReferencesInError}");
                        }
                        else
                        {
                            _logger.LogInformation($"completed treatment for message: {messageId}. {categoriseDocumentMessage.ListOfDocReferences.Count} information assets processed.");
                        }
                    }     
                }
                else
                {
                    _tcs.SetCanceled();
                }
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                _logger.LogCritical($"Fatal Error: {e.Message}" );

                Exception ie = e.InnerException;
                do
                {
                    _logger.LogCritical($"- {ie.Message}");
                    ie = ie.InnerException;
                } while (ie != null);

                throw;
            }
        }
    }
}
