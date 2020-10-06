using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;

namespace bt_sql_backup_service
{
  /// <summary>
  /// A job that runs a SQL command as defined by an instance of a <see
  /// cref="SchedulableSQLCommand">SchedulableSQLCommand</see>.
  /// </summary>
  /// <remarks>
  /// The instance of <see cref="SchedulableSQLCommand">SchedulableSQLCommand</see>
  /// must be passed via the job execution context object.
  /// </remarks>
  public class RunSqlCommandJob : IJob
  {
    public async Task Execute(IJobExecutionContext context)
    {
      SchedulableSQLCommand command = (SchedulableSQLCommand)context.JobDetail.JobDataMap.Get("command");
      ILogger logger = (ILogger)context.JobDetail.JobDataMap.Get("logger");
      if (command == null)
      {
        logger?.LogError("Failed to execute a job. The JobDataMap returned a null value for the key 'command'.");
        return;
      }

      logger?.LogInformation($"Executing job '{command.Name}': {command.Description}");
      using (SqlConnection connection = new SqlConnection(command.ConnectionString))
      {
        try
        {
          await connection.OpenAsync();
          using (SqlCommand sqlCommand = connection.CreateCommand())
          {
            sqlCommand.CommandType = System.Data.CommandType.Text;
            sqlCommand.CommandText = command.SqlCommand;
            sqlCommand.CommandTimeout = command.CommandTimeout;

            int rowsAffected = await sqlCommand.ExecuteNonQueryAsync();
            logger?.LogInformation($"Finished executing job '{command.Name}'. {rowsAffected} rows affected.");
          }
        }
        catch (System.Exception e)
        {
          var message = e.Message.Replace("\r\n", " ").Replace('\n', ' ');
          logger.LogError($"Error while executing job '{command.Name}': {message}");
        }
      }
    }
  }
}