using Microsoft.VisualStudio.TestTools.UnitTesting;
using NationalArchives.Taxonomy.Common.BusinessObjects;
using NationalArchives.Taxonomy.Common.Service;
using NationalArchives.Taxonomy.Batch.DailyUpdate.MessageQueue;
using NSubstitute;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.DailyUpdate.MesssageQueue;
using NationalArchives.Taxonomy.Batch.Service;
using NationalArchives.Taxonomy.Common.Service.Interface;
using System.Collections.Generic;

namespace NationalArchives.Taxonomy.Batch
{
    [TestClass]
    public class UnitTest1
    {
        ICategoriserService<CategorisationResult> categoriserService = Substitute.For<ICategoriserService<CategorisationResult>>();
        MessageQueueParams inputMsgQueueParams = Substitute.For<MessageQueueParams>();
        ILogger<ActiveMqConsumerBase> logger = Substitute.For<ILogger<ActiveMqConsumerBase>>();
        ILogger<DailyUpdatesManagerService> _logger1 = Substitute.For<ILogger<DailyUpdatesManagerService>>();

        ISourceIaidInputQueueConsumer _iaidConsumer = Substitute.For<ISourceIaidInputQueueConsumer>();

        IUpdateOpenSearchService _updateOpenSearchService = Substitute.For<IUpdateOpenSearchService>();


        //[TestMethod]
        //public void TestDailyUpdatesService()
        //{
        //    // Not much to test really - just to start and stop service without error.  
        //    // The legacy ActiveMQ consumer is tricky to test  because we're passing a URI directly rather
        //    // than ActiveMQConnectionFactory (see https://activemq.apache.org/how-to-unit-test-jms-code).
        //    // So you would need to have ActiveMQ actually installed to test properly.
        //    CategorisationParams catParams = new CategorisationParams() { CategoriserStartDelay = 1000, BatchSize = 50, LogEachCategorisationResult = true };
        //    var consumers = new List<ISourceIaidInputQueueConsumer>();

        //    consumers.Add(_iaidConsumer);
        //    var dailyUpdateService = new DailyUpdatesManagerService(consumers, _logger1);
        //    dailyUpdateService.StartAsync(new System.Threading.CancellationToken());
        //    dailyUpdateService.StopAsync(new System.Threading.CancellationToken());
        //    dailyUpdateService.Dispose();
        //}
    }
}
