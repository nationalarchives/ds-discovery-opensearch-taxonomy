using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.Service.Interface;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.Update.Elastic.Service
{
    internal class UpdateElasticWindowsService : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IUpdateElasticService _updateElasticService;
        private readonly ILogger<UpdateElasticWindowsService> _logger;

        public UpdateElasticWindowsService(IUpdateElasticService updateElasticService, ILogger<UpdateElasticWindowsService> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _updateElasticService = updateElasticService;
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {

            try
            {
                _logger.LogInformation(Properties.Resources.FlushRemaingUpdatesToElasticMsg);
                _updateElasticService.Flush();
                _logger.LogInformation("Stopping the Elastic Update Windows Service.");

                base.StopAsync(cancellationToken);

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return Task.FromException(e);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
            _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

            Task updateTask = Task.Run(() => _updateElasticService.Init());
            TaskAwaiter awaiter = updateTask.GetAwaiter();

            awaiter.OnCompleted(() => OutputCompletion(updateTask));

            return Task.CompletedTask;
        }

        private void OutputCompletion(Task task)
        {
            if (task.IsCanceled)
            {
                _logger.LogInformation("Elastic search update service is stopping due to cancellation.");
            }
            else if (task.IsFaulted)
            {
                _logger.LogError("The Elastic search update service is stopping due to an exception.");

            }
            else
            {
                _logger.LogInformation("Processing of Elastic search updates completed.");
                _logger.LogInformation("The Elastic search updates service is stopping.");
            }
            _hostApplicationLifetime.StopApplication();
        }

        private void OnStarted()
        {
            _logger.LogInformation("Taxonomy Elastic Search Updates Service has started.");
        }

        private void OnStopping()
        {
            _logger.LogInformation("Taxonomy Elastic Search Updates Service is stopping.");
        }

        private void OnStopped()
        {
            _logger.LogInformation("Taxonomy Elastic Search Updates Service is stopped.");
        }
    }
}
