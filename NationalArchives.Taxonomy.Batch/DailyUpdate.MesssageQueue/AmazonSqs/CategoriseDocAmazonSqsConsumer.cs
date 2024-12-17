using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.Utils;
using NationalArchives.Taxonomy.Common;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Domain.Queue;
using NationalArchives.Taxonomy.Common.Service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue
{
    internal sealed class CategoriseDocAmazonSqsConsumer : AmazonSqsConsumerBase
    {
        private readonly ICategoriserService<CategorisationResult> _categoriserService;

        public CategoriseDocAmazonSqsConsumer(ICategoriserService<CategorisationResult> categoriserService,
            DailyUpdateQueueParams dailyUpdatequeueParams, ILogger<CategoriseDocAmazonSqsConsumer> logger) : base(dailyUpdatequeueParams.AmazonSqsParams, logger)
        {
            _categoriserService = categoriserService;
        }

        protected override async Task HandleTextMessage(IList<string> iaids)
        {
            try
            {
                if (!_token.IsCancellationRequested)
                {
                    
                    if (iaids == null || iaids.Count == 0)
                    {
                        _tcsInit.SetResult(null);
                    }
                    else
                    {
                        string summaryMessage = $"received Categorise Document message from daily update queue, docReferences: {String.Join(',', iaids)}, total = {iaids.Count}";
                        _logger.LogInformation(summaryMessage);
                        Console.WriteLine(summaryMessage);

                        var categoriseDocumentMessage = new TaxonomyDocumentMessageHolder(iaids);

                        foreach (string iaid in categoriseDocumentMessage.ListOfDocReferences)
                        {
                            try
                            {
                                IList<CategorisationResult> results = await _categoriserService.CategoriseSingle(iaid);

                                int numberOfCategoriesMatched = results.Count;
                                _logger.LogInformation($"  - Categorised {iaid}, {numberOfCategoriesMatched} " + (numberOfCategoriesMatched == 1 ? "category" : "categories") 
                                    + $" found: " + String.Join(';', results));

                                allIaidsReceived.Add(iaid);
                            }
                            catch (TaxonomyException e)
                            {
                                categoriseDocumentMessage.AddDocReferenceInError(iaid);
                                _logger.LogError(e, $"error processing iaid {iaid} from message queue.");
                            }
                            catch (Exception e)
                            {
                                categoriseDocumentMessage.AddDocReferenceInError(iaid);
                                _logger.LogError(e, $"error processing iaid {iaid} from message queue.");
                                TaxonomyException te = new TaxonomyException(TaxonomyErrorType.CATEGORISATION_ERROR, $"Error processing iaid {iaid} from message queue.", e);
                                throw te;
                            }
                        }

                        if (categoriseDocumentMessage.HasProcessingErrors)
                        {
                            _logger.LogWarning($"completed treatment for daily update message with {categoriseDocumentMessage.ListOfDocReferencesInError} errors.");
                            _logger.LogWarning($"DOCREFERENCES THAT COULD NOT BE CATEGORISED: {categoriseDocumentMessage.ListOfDocReferencesInError}");
                        }
                        else
                        {
                            _logger.LogInformation($"completed treatment for message from dauly update queue. {categoriseDocumentMessage.ListOfDocReferences.Count} information assets processed.");
                        }
                    }
                }
                else
                {
                    _tcsInit.SetCanceled();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, $"Fatal Error: {ex.Message}");
                _tcsInit.SetException(ex);
                throw;
            }
        }
    }
}
