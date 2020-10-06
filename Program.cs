using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Topshelf;

namespace bt_sql_backup_service
{
  public class BtSqlBackupService : ServiceControl
  {
    private readonly string appDirectoryPath;
    private readonly string sqlCommandsFilePath;
    private readonly IScheduler scheduler;
    private readonly List<SchedulableSQLCommand> sqlCommands;
    private readonly ILogger logger;

    public BtSqlBackupService()
    {
      var loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.AddFilter("Microsoft", LogLevel.Warning)
               .AddFilter("System", LogLevel.Warning)
               .AddFilter("bt_sql_backup_service.BtSqlBackupService", LogLevel.Debug)
               .AddFilter("Default", LogLevel.Information)
               .AddConsole(opt =>
               {
                 opt.TimestampFormat = "dd-MM-yyyy H:mm:ss.ffff ";
               })
               .AddEventLog();
      });
      this.logger = loggerFactory.CreateLogger<BtSqlBackupService>();
      this.appDirectoryPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
      this.sqlCommandsFilePath = Path.Combine(this.appDirectoryPath, "sql_commands.json");
      this.sqlCommands = new List<SchedulableSQLCommand>();
      this.scheduler = new StdSchedulerFactory().GetScheduler().GetAwaiter().GetResult();
    }

    private async Task ScheduleJobs()
    {
      foreach (var command in this.sqlCommands)
      {
        logger.LogInformation($"Scheduling job '{command.Name}'.");

        var data = new JobDataMap();
        data.Add("command", command);
        data.Add("logger", this.logger);

        var jobBuilder = JobBuilder
        .Create<RunSqlCommandJob>()
        .WithIdentity(command.Name, command.ConnectionString)
        .WithDescription(command.Description)
        .SetJobData(data);

        var jobDetail = jobBuilder.Build();

        var jobTrigger = TriggerBuilder.Create()
        .WithIdentity(command.Name, command.ConnectionString)
        .WithDescription(command.Description)
        .WithCronSchedule(command.Cron)
        .StartNow()
        .Build();

        await this.scheduler.ScheduleJob(jobDetail, jobTrigger);
      }
    }

    /// <summary>
    /// Load jobs from configuration file on disk.
    /// </summary>
    /// <returns>true if successful</returns>
    private bool LoadJobs()
    {
      logger.LogInformation($"Loading jobs from '{this.sqlCommandsFilePath}'.");

      var serializerOptions = new JsonSerializerOptions
      {
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      };

      try
      {
        var json = File.ReadAllText(this.sqlCommandsFilePath);
        var jobs = JsonSerializer.Deserialize<SchedulableSQLCommand[]>(json, serializerOptions);
        this.sqlCommands.AddRange(jobs);
      }
      catch (System.Exception e)
      {
        logger.LogError($"Failed to load jobs from '{this.sqlCommandsFilePath}': {e.Message}");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Loads the jobs and starts the scheduler.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>true if successful</returns>
    private async Task<bool> StartAsync(HostControl hostControl)
    {
      if (!this.LoadJobs())
      {
        // Failed to load jobs from disk so don't start the service.
        return false;
      }
      await this.ScheduleJobs();
      await this.scheduler.Start();
      return true;
    }

    /// <summary>
    /// Starts the service.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>true if started successfully</returns>
    public bool Start(HostControl hostControl)
    {
      logger.LogInformation("Starting service.");

      // Queue an async task to run on a thread pool, which loads necessary data
      // and starts the scheduler. We don't wait here for this process to
      // complete, because it can take longer than the service startup timeout,
      // which could terminate the service before it can even start.
      Task.Run(() => this.StartAsync(hostControl).ContinueWith((startTask) =>
      {
        if (!startTask.Result)
        {
          // Gracefully stop the service because we failed to start
          // asynchronously.
          this.Stop(hostControl);
        }
      }));

      return true;
    }

    /// <summary>
    /// Stops the scheduler.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>Always true</returns>
    private async Task<bool> StopAsync(HostControl hostControl)
    {
      await scheduler.Shutdown();
      return true;
    }

    /// <summary>
    /// Stops the service.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>true if stopped successfully</returns>
    public bool Stop(HostControl hostControl)
    {
      logger.LogInformation("Stopping service.");
      return this.StopAsync(hostControl).GetAwaiter().GetResult();
    }
  }

  class Program
  {
    public static int Main(string[] args)
    {
      return (int)HostFactory.Run(hostConfigurator =>
         {
           hostConfigurator.Service<BtSqlBackupService>();
           hostConfigurator.RunAsNetworkService();
           hostConfigurator.StartAutomatically();
           hostConfigurator.SetStartTimeout(TimeSpan.FromSeconds(30));
           hostConfigurator.SetDisplayName(Constants.displayName);
           hostConfigurator.SetServiceName(Constants.serviceName);
           hostConfigurator.SetDescription(Constants.description);
           hostConfigurator.EnableServiceRecovery(action => action.RestartService(TimeSpan.FromSeconds(60)));
         }
      );
    }
  }
}
