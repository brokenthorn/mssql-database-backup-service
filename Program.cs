using System;
using System.IO;
using System.Data;
using Microsoft.Data.SqlClient;
using Topshelf;

namespace bt_sql_backup_service
{
  public static class Constants
  {
    /// <summary>
    /// Windows Service Name Identifier for this service.
    /// </summary>
    public const string serviceName = "BtSqlBackupService";

    /// <summary>
    /// Windows Service Description field information for this service.
    /// </summary>
    public const string description = "BrokenThorn's SQL Backup Service";

    /// <summary>
    /// Windows Service Display Name used to list this service in the Services Manager.
    /// </summary>
    public const string displayName = "SQL Backup Service";
  }

  public class BtSqlBackupService : ServiceControl
  {
    // TODO: Runtime configuration with default fallback for log file location.
    private static readonly string _logFileLocation = $@"C:\temp\{Constants.serviceName}.log.txt";

    private static string GetConnectionString()
    {
      // TODO: Runtime configuration of SQL server connection string.
      // WARN: Avoid storing the connection string in code!
      // Retrieve it from a configuration file.
      return "Server=127.0.0.1,1443;Database=YourDatabaseName;User Id=YourSqlUseName;Password=YourSqlPassword;";
    }

    // TODO: Implement proper backup methods: to disk, to Azure Blob Storare, copy only, etc.
    private void BackupDatabaseToDisk()
    {
      try
      {
        this.Log("Preiau string-ul de conectare la serverul SQL.");

        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(GetConnectionString());
        string connectionString = builder.ConnectionString;

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
          try
          {
            this.Log($"Deschid conexiune cu serverul: {connectionString}.");
            connection.Open();

            this.Log($"INFO: WorkstationId: { connection.WorkstationId}");
            this.Log($"INFO: ServerVersion: { connection.ServerVersion}");
            this.Log($"IFNO: State: {connection.State}");

            this.Log($"Inchid conexiunea cu serverul: {connectionString}.");
          }
          catch (System.Exception e)
          {
            this.Log($"Eroare la conectarea la serverul SQL: {e.Message}");
          }
        }
      }
      catch (System.Exception e)
      {
        this.Log($"Eroare la interpretarea string-ului de conectare la server-ul SQL: {e.Message}");
      }
    }

    private void Log(string logMessage)
    {
      try
      {
        Directory.CreateDirectory(Path.GetDirectoryName(_logFileLocation));
      }
      catch (System.Exception e)
      {
        Console.WriteLine(logMessage);

        Console.Error.WriteLine($"A aparut o eroare la crearea folderului pentru stocare log-urilor {_logFileLocation}!");
        Console.Error.WriteLine(e);

        return;
      }

      try
      {
        File.AppendAllText(_logFileLocation, DateTime.UtcNow.ToString() + " : " + logMessage + Environment.NewLine);
      }
      catch (System.Exception e)
      {
        Console.WriteLine(logMessage);

        Console.Error.WriteLine($"A aparut o eroare la scriere in fisierul de log {_logFileLocation}!");
        Console.Error.WriteLine(e);
      }
    }

    public bool Start(HostControl hostControl)
    {
      Log("Porneste service-ul.");
      this.BackupDatabaseToDisk();

      return true;
    }

    public bool Stop(HostControl hostControl)
    {
      Log("Se opreste service-ul.");

      return true;
    }
  }

  class Program
  {
    static int Main(string[] args)
    {
      return (int)HostFactory.Run(sc =>
         {
           sc.Service<BtSqlBackupService>();
           sc.EnableServiceRecovery(r => r.RestartService(TimeSpan.FromSeconds(10)));
           sc.SetDisplayName(Constants.displayName);
           sc.SetServiceName(Constants.serviceName);
           sc.SetDescription(Constants.description);
           sc.StartAutomatically();
         }
      );
    }
  }
}
