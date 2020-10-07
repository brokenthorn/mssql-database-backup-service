using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using BtSqlBackupService.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BtSqlBackupService
{
  /// <summary>
  ///   A job that runs a SQL command as defined by an instance of a
  ///   <see cref="SqlCommandEntity" />.
  /// </summary>
  /// <remarks>
  ///   The instance of <see cref="SqlCommandEntity" /> must be passed via the
  ///   job execution context object.
  /// </remarks>
  public class RunSqlCommandJob : IJob
  {
    public async Task Execute(IJobExecutionContext context)
    {
      var sqlCommandEntity =
        (SqlCommandEntity) context.JobDetail.JobDataMap.Get("command");
      var logger = (ILogger) context.JobDetail.JobDataMap.Get("logger");
      var mailer = (Mailer) context.JobDetail.JobDataMap.Get("mailer");

      if (sqlCommandEntity == null || logger == null || mailer == null)
      {
        logger?.LogError(
          "Failed to execute a job. The JobDataMap didn't return non-null values for some keys.");
        return;
      }

      logger?.LogInformation(
        $"Executing job '{sqlCommandEntity.Name}': {sqlCommandEntity.Description}");
      
      // await mailer.SendEmailAsync
      // (
      //   sqlCommandEntity.NotifyEmails,
      //   $"[{Environment.MachineName}] Executing job {sqlCommandEntity.Name}",
      //   $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:<br/>SQL Backup Service is executing the '{sqlCommandEntity.Name}' job on {Environment.MachineName} running {Environment.OSVersion}.<br/><br/>Job description: {sqlCommandEntity.Description}"
      // );

      using (var conn = new SqlConnection(sqlCommandEntity.ConnectionString))
      {
        try
        {
          await conn.OpenAsync();
          using (var sqlCommand = conn.CreateCommand())
          {
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.CommandText = sqlCommandEntity.SqlCommand;
            sqlCommand.CommandTimeout = sqlCommandEntity.CommandTimeout;
            var rowsAffected = await sqlCommand.ExecuteNonQueryAsync();
            
            logger?.LogInformation(
              $"Finished executing job '{sqlCommandEntity.Name}'. {rowsAffected} rows affected.");
          }
        }
        catch (Exception e)
        {
          var message = e.Message.Replace("\r\n", " ").Replace('\n', ' ');
          
          logger.LogError(
            $"Error while executing job '{sqlCommandEntity.Name}': {message}");
          
          await mailer.SendEmailAsync
          (
            sqlCommandEntity.NotifyEmails,
            $"[{Environment.MachineName}] Error while executing job",
            $"{DateTime.Now.ToString(CultureInfo.CurrentCulture)}:<br/>SQL Backup Service encountered an error while executing the '{sqlCommandEntity.Name}' job on {Environment.MachineName} running {Environment.OSVersion}:<br/>{message}"
          );
        }
      }
    }
  }
}