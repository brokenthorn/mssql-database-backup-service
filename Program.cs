#define TRACE

using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Topshelf;

namespace bt_sql_backup_service
{
  public class BtSqlBackupService : ServiceControl, IDisposable
  {
    private readonly string appDirectory = Directory.GetCurrentDirectory();
    private readonly string logFilePath;
    private readonly string settingsFileDirectory;
    private readonly string settingsFilePath;

    private readonly IScheduler scheduler;
    private readonly List<SQLCommandJob> sqlCommands = new List<SQLCommandJob>();
    private readonly TraceSource trace;

    public BtSqlBackupService()
    {
      this.settingsFilePath = Path.Combine(this.appDirectory, $"{Constants.serviceName}.json");
      this.settingsFileDirectory = Path.GetDirectoryName(this.settingsFilePath);
      this.logFilePath = Path.Combine(this.appDirectory, $"{Constants.serviceName}.log.csv");

      this.trace = new TraceSource("BtSqlBackupService");

      var sourceSwitch = new SourceSwitch("BtSqlBackupService", "Verbose");
      sourceSwitch.Level = SourceLevels.Verbose;
      this.trace.Switch = sourceSwitch;

      var consoleTraceListener = new ConsoleTraceListener();
      consoleTraceListener.Name = "console";
      consoleTraceListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId;
      this.trace.Listeners.Add(consoleTraceListener);

      var fileTraceListener = new DelimitedListTraceListener(this.logFilePath, "logFile");
      fileTraceListener.Name = "logFile";
      fileTraceListener.TraceOutputOptions = TraceOptions.DateTime | TraceOptions.ProcessId | TraceOptions.ThreadId;
      this.trace.Listeners.Add(fileTraceListener);

      this.trace.TraceInformation($"New instance of {this.GetType().Name}");

      StdSchedulerFactory factory = new StdSchedulerFactory();
      var getSchedulerTask = factory.GetScheduler();
      getSchedulerTask.Wait();
      this.scheduler = getSchedulerTask.Result;
    }

    /// <summary>
    /// Schedules the jobs found in the configuration file.
    /// </summary>
    private async Task ScheduleJobs()
    {
      foreach (SQLCommandJob cmd in this.sqlCommands)
      {
        this.trace.TraceInformation($"Creating and scheduling a new job from a `SQLCommandJob` type object (Name='{cmd.Name}').");

        JobDataMap jobData = new JobDataMap();
        jobData.Add("sqlCommand", cmd);
        jobData.Add("trace", this.trace);

        JobBuilder sqlJobBuilder = JobBuilder
        .Create<RunSqlCommandJob>()
        .WithIdentity(cmd.Name, cmd.ConnectionString)
        .WithDescription("Job builder pentru comenzi SQL pe MSSQL Server.")
        .SetJobData(jobData);

        IJobDetail sqlJobDetail = sqlJobBuilder.Build();
        ITrigger sqlJobTrigger = TriggerBuilder.Create()
        .WithIdentity(cmd.Name, cmd.ConnectionString)
        .WithDescription(cmd.Description)
        .WithCronSchedule(cmd.Cron)
        .StartNow()
        .Build();

        await this.scheduler.ScheduleJob(sqlJobDetail, sqlJobTrigger);
      }
    }

    private bool loadSettingsFromDisk()
    {
      var jsonSerializerOptions = new JsonSerializerOptions
      {
        AllowTrailingCommas = true,
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      };

      try
      {
        this.trace.TraceInformation($"Se incarca setarile din fisierul {this.settingsFilePath}.");

        this.sqlCommands.Clear();
        var jsonString = File.ReadAllText(this.settingsFilePath);
        this.sqlCommands.AddRange(JsonSerializer.Deserialize<SQLCommandJob[]>(jsonString, jsonSerializerOptions));
      }
      catch (System.Exception e)
      {
        this.trace.TraceEvent(TraceEventType.Error, 0, $"Eroare la incarcarea setarilor: {e.Message}");
        return false;
      }

      return true;
    }

    private async Task<bool> StartAsync(HostControl hostControl)
    {
      this.trace.TraceInformation("Se porneste serviciul.");
      if (!this.loadSettingsFromDisk())
      {
        return false;
      }
      await this.ScheduleJobs();
      await this.scheduler.Start();
      return true;
    }

    public bool Start(HostControl hostControl)
    {
      var startTask = this.StartAsync(hostControl);
      return true;
    }

    private async Task<bool> StopAsync(HostControl hostControl)
    {
      this.trace.TraceInformation("Se opreste serviciul.");
      await scheduler.Shutdown();
      return true;
    }

    public bool Stop(HostControl hostControl)
    {
      var stopTask = this.StopAsync(hostControl);
      stopTask.Wait();
      return stopTask.Result;
    }

    public void Dispose()
    {
      this.trace.Close();
    }
  }

  class Program
  {
    public static int Main(string[] args)
    {
      int retValue = (int)HostFactory.Run(hostConfigurator =>
         {
           hostConfigurator.Service<BtSqlBackupService>();
           hostConfigurator.RunAsNetworkService();
           hostConfigurator.StartAutomatically();
           hostConfigurator.SetStartTimeout(TimeSpan.FromSeconds(60));
           hostConfigurator.SetDisplayName(Constants.displayName);
           hostConfigurator.SetServiceName(Constants.serviceName);
           hostConfigurator.SetDescription(Constants.description);
           hostConfigurator.EnableServiceRecovery(action => action.RestartService(TimeSpan.FromSeconds(60)));
         }
      );

      return retValue;
    }
  }
}
