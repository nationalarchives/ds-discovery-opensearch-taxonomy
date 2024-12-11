using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NationalArchives.Taxonomy.Common.Service.Interface;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NationalArchives.Taxonomy.Batch.Update.OpenSearch.Service
{
    internal class UpdateOpenSearchWindowsService : BackgroundService
    {
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IUpdateOpenSearchService _updateOpenSearchService;
        private readonly ILogger<UpdateOpenSearchWindowsService> _logger;

        public UpdateOpenSearchWindowsService(IUpdateOpenSearchService updateOpenSearchService, ILogger<UpdateOpenSearchWindowsService> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _updateOpenSearchService = updateOpenSearchService;
            _logger = logger;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {

            try
            {
                _logger.LogInformation(Properties.Resources.FlushRemaingUpdatesToOpenSearchMsg);
               _updateOpenSearchService.Flush().Wait(TimeSpan.FromMinutes(2));
                _logger.LogInformation("Stopping the Open Search Update Windows Service.");

                base.StopAsync(cancellationToken);

                return Task.CompletedTask;
            }
            catch (Exception e)
            {
               _logger.LogError(e, "Exception occurre whilst attempting to stop the Taxonomy Update Service");
                return Task.FromException(e);
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _hostApplicationLifetime.ApplicationStarted.Register(OnStarted);
            _hostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _hostApplicationLifetime.ApplicationStopped.Register(OnStopped);

            Task updateTask =  _updateOpenSearchService.Init();
            TaskAwaiter awaiter = updateTask.GetAwaiter();

            awaiter.OnCompleted(() => OutputCompletion(updateTask));

            updateTask.Wait();
            return Task.CompletedTask;
        }

        private void OutputCompletion(Task task)
        {
            if (task.IsCanceled)
            {
                _logger.LogInformation("Open Search update service is stopping due to cancellation.");
            }
            else if (task.IsFaulted)
            {
                _logger.LogError(task.Exception, "The Open Search update service is stopping due to an exception.");
                foreach(Exception ex in task.Exception.Flatten().InnerExceptions )
                {
                    _logger.LogError(ex, "Inner Exception");
                }
            }
            else
            {
                _logger.LogInformation("Processing of Open Search updates completed.");
                _logger.LogInformation("The Open Search updates service is stopping.");
            }
            _hostApplicationLifetime.StopApplication();
        }

        private void OnStarted()
        {
            _logger.LogInformation("Taxonomy Open Search Updates Service has started.");
        }

        private void OnStopping()
        {
            _logger.LogInformation("Taxonomy Open Search Updates Service is stopping.");
        }

        private void OnStopped()
        {
            _logger.LogInformation("Taxonomy Open Search Updates Service is stopped.");
        }
    }
}
