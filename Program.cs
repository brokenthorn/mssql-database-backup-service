using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BtSqlBackupService.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Topshelf;

namespace BtSqlBackupService
{
  public class BtSqlBackupService : ServiceControl
  {
    private static readonly IConfiguration AppSettingsConfiguration =
      new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false, true)
        .AddEnvironmentVariables()
        .Build();

    private static readonly SmtpClientSettings SmtpClientSettings =
      AppSettingsConfiguration
        .GetSection("SmtpClientSettings")
        .Get<SmtpClientSettings>();

    private static readonly string AppDirPath =
      Path.GetDirectoryName(
        Assembly.GetEntryAssembly()?.Location
      );

    /// <summary>
    ///   Full path to sql_commands.json file.
    /// </summary>
    private static readonly string SqlCommandsFilePath =
      Path.Combine(AppDirPath, "sql_commands.json");

    /// <summary>
    ///   Static <see cref="ILogger" /> instance for app-wide general logging.
    /// </summary>
    /// <remarks>
    ///   Logging configuration and filtering is loaded from appsettings.json.
    ///   A console logger and a Window Event Log logger are added by default.
    /// </remarks>
    private static readonly ILogger<BtSqlBackupService> Logger =
      LoggerFactory.Create(builder =>
      {
        builder
          .AddConfiguration(AppSettingsConfiguration)
          .AddConsole(opt =>
          {
            opt.TimestampFormat = "dd-MM-yyyy H:mm:ss.ffff ";
          })
          .AddEventLog(); // Not supported on non-Windows targets!
      }).CreateLogger<BtSqlBackupService>();

    private readonly Mailer _mailer;

    /// <summary>
    ///   Quartz <see cref="IScheduler" /> instance.
    /// </summary>
    private readonly IScheduler _scheduler;

    /// <summary>
    ///   A list of the <see cref="SqlCommandEntity" /> that will be
    ///   scheduled to run by this service.
    /// </summary>
    private readonly List<SqlCommandEntity> _sqlCommands;

    public BtSqlBackupService()
    {
      _sqlCommands = new List<SqlCommandEntity>();
      _mailer = new Mailer(SmtpClientSettings, Logger);
      _scheduler = new StdSchedulerFactory()
        .GetScheduler()
        .GetAwaiter()
        .GetResult();
    }

    /// <summary>
    ///   Starts the service.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>true if started successfully</returns>
    public bool Start(HostControl hostControl)
    {
      Logger.LogInformation("Starting service.");

      // Queue this task to run on a separate thread so the service isn't killed
      // by the service startup timeout limit.
      Task.Run(() => StartAsync(hostControl).ContinueWith(startTask =>
      {
        if (!startTask.Result)
          // Gracefully stop the service because we failed to start
          // asynchronously.
          Stop(hostControl);
      }));

      return true;
    }

    /// <summary>
    ///   Stops the service.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>true if stopped successfully</returns>
    public bool Stop(HostControl hostControl)
    {
      Logger.LogInformation("Stopping service.");
      return StopAsync(hostControl).GetAwaiter().GetResult();
    }

    /// <summary>
    ///   Schedules <see cref="SqlCommandEntity" />s.
    /// </summary>
    /// <returns></returns>
    private async Task ScheduleSqlCommands()
    {
      foreach (var command in _sqlCommands)
      {
        Logger.LogInformation($"Scheduling SQL command job '{command.Name}'.");

        // Give the job the SQL command and logger (available in its context).
        var data = new JobDataMap
        {
          {"command", command},
          {"logger", Logger},
          {"mailer", _mailer}
        };
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

        await _scheduler.ScheduleJob(jobDetail, jobTrigger);
      }
    }

    /// <summary>
    ///   Loads <see cref="SqlCommandEntity" /> jobs from the file represented by
    ///   the <see cref="SqlCommandsFilePath" /> property.
    /// </summary>
    /// <returns>true if successful</returns>
    private bool LoadSqlCommandJobs()
    {
      Logger.LogInformation(
        $"Loading SQL command jobs from '{SqlCommandsFilePath}'.");

      var serializerOptions = new JsonSerializerOptions
      {
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      };
      try
      {
        var json = File.ReadAllText(SqlCommandsFilePath);
        var jobs =
          JsonSerializer.Deserialize<SqlCommandEntity[]>(json,
            serializerOptions);
        _sqlCommands.AddRange(jobs);
      }
      catch (Exception e)
      {
        Logger.LogError(
          $"Failed to load SQL command jobs from '{SqlCommandsFilePath}': {e.Message}");
        return false;
      }

      return true;
    }

    /// <summary>
    ///   Loads the jobs and starts the scheduler.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>true if successful</returns>
    private async Task<bool> StartAsync(HostControl hostControl)
    {
      await _mailer.SendEmailAsync
      (
        SmtpClientSettings.AdminEmail,
        $"[{Environment.MachineName}] Starting SQL Backup Service",
        $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:<br/>Starting SQL Backup Service on {Environment.MachineName} running {Environment.OSVersion}."
      );

      if (!LoadSqlCommandJobs())
      {
        await _mailer.SendEmailAsync
        (
          SmtpClientSettings.AdminEmail,
          $"[{Environment.MachineName}] Failed to start the service: Failed to load jobs.",
          $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:<br/>Failed to start the service: Failed to load jobs on {Environment.MachineName} running {Environment.OSVersion}."
        );
        // Failed to load jobs from disk so don't start the service.
        return false;
      }

      await ScheduleSqlCommands();
      await _scheduler.Start();
      return true;
    }

    /// <summary>
    ///   Stops the scheduler.
    /// </summary>
    /// <param name="hostControl"></param>
    /// <returns>Always true</returns>
    private async Task<bool> StopAsync(HostControl hostControl)
    {
      await _mailer.SendEmailAsync
      (
        SmtpClientSettings.AdminEmail,
        $"[{Environment.MachineName}] Stopping SQL Backup Service",
        $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:<br/>Stopping SQL Backup Service on {Environment.MachineName} running {Environment.OSVersion}."
      );

      await _scheduler.Shutdown();
      return true;
    }
  }

  internal static class Program
  {
    public static int Main(string[] args)
    {
      return (int) HostFactory.Run(hostConfigurator =>
        {
          hostConfigurator.Service<BtSqlBackupService>();
          hostConfigurator.RunAsNetworkService();
          hostConfigurator.StartAutomatically();
          hostConfigurator.SetStartTimeout(TimeSpan.FromSeconds(30));
          hostConfigurator.SetDisplayName(Constants.displayName);
          hostConfigurator.SetServiceName(Constants.serviceName);
          hostConfigurator.SetDescription(Constants.description);
          hostConfigurator.EnableServiceRecovery(action =>
            action.RestartService(TimeSpan.FromSeconds(60)));
        }
      );
    }
  }
}