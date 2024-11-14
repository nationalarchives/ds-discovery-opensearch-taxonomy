using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Batch.DailyUpdate.MesssageQueue;
using NationalArchives.Taxonomy.Common;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.Service
{
    internal sealed class DailyUpdatesManagerService : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IEnumerable<ISourceIaidInputQueueConsumer> _updateMessageQueueConsumers;
        private readonly ILogger<DailyUpdatesManagerService> _logger;
        private Timer _timer;

        private CancellationTokenSource _dailyUpdatesCancelledSource = new CancellationTokenSource();

        private volatile int _iaidCount = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateMessageQueueConsumer"></param>
        /// <param name="logger"></param>
        /// <param name="deleteMsgQueueConsumer"></param>
        public DailyUpdatesManagerService(IEnumerable<ISourceIaidInputQueueConsumer> updateMessageQueueConsumers, ILogger<DailyUpdatesManagerService> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _updateMessageQueueConsumers = updateMessageQueueConsumers ?? throw new TaxonomyException("Update message queue is required!");
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

 

        private void OutputCompletion(Task task)
        {
            if (task.IsCanceled)
            {
                _logger.LogInformation("Daily update process cancelled.");
                _logger.LogError("The daily update service is stopping due to cancellation.");
            }
            else if (task.IsFaulted)
            {
                _logger.LogError(task.Exception?.InnerException?.Message);
                _logger.LogError(task.Exception?.InnerException?.StackTrace);
                _logger.LogError("Fatal exception occured during processing of daily updates, please check the logs for details.");
                _logger.LogError("The daily update service is stopping due to an exception.");

            }
            else
            {
                _logger.LogInformation("Processing of daily updates completed.");
                _logger.LogError("The daily update service is stopping.");
            }
            StopAsync(_dailyUpdatesCancelledSource.Token);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                base.StopAsync(cancellationToken);
                _logger.LogInformation("Stopping Daily Updates Manager Service.");
                this.Dispose();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Task.FromException(e);
            }
        }

        public override void Dispose()
        {
            _timer.Dispose();
            foreach(var consumer in _updateMessageQueueConsumers)
            {
                consumer?.Dispose();
            }

            _dailyUpdatesCancelledSource?.Dispose();
        }

        private void CategoriseDocMessageConsumer_FatalException(object sender, MessageProcessingEventArgs e)
        {
            Console.WriteLine("Fatal exception occured during processing of daily updates, please check the logs for details.");
            StopAsync(new CancellationToken());
        }

        private void CategoriseDocMessageConsumer_ProcessingCompleted(object sender, MessageProcessingEventArgs e)
        {
            try
            {
                _logger.LogInformation(e.Message);
                StopAsync(new CancellationToken());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting the Daily Updates Manager Service");

            _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
            _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

            foreach (var consumer in _updateMessageQueueConsumers)
            {

                //TODO: Currently the task will keep runnng until the input queue
                // is sent an empty message (or one with from which no IAIDs are extracted).
                // This will then set the task as complete (and stop the service).
                // Otherwise the service will keep running e.g. until the next day's updates
                // Possibly we want to end and restart the task from here on a daily schedule.
                // This would require the input queue to have a completion signal as per above.

                Task task = Task.Run(() => consumer.Init(_dailyUpdatesCancelledSource.Token), _dailyUpdatesCancelledSource.Token);
                TaskAwaiter awaiter = task.GetAwaiter();

                awaiter.OnCompleted
                (
                    () => OutputCompletion(task)
                );
            }

            // For console/log output updates every minute:
            new Thread(() =>
            {

                _timer = new Timer(
                    (e) =>
                    {
                        _iaidCount = 0;
                        foreach (var consumer in _updateMessageQueueConsumers)
                        {
                            _iaidCount += consumer.IaidCount;
                        }
                        Console.WriteLine($"Still listening!  Iaids updated: {_iaidCount}.");
                    }
                    ,
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromMinutes(1));

            }
            ).Start();


            return Task.CompletedTask;
        }

        private void OnStarted()
        {
            _logger.LogInformation("Taxonomy Generator Daily Updates Service has started.");

        }

        private void OnStopping()
        {

            _logger.LogInformation("Taxonomy Generator Daily Updates Service is stopping.");
        }

        private void OnStopped()
        {
            _logger.LogInformation("Taxonomy Generator Daily Updates Service is stopped.");
        }
    } 
}