using Microsoft.Data.SqlClient;
using Quartz;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace bt_sql_backup_service
{
  public class RunSqlCommandJob : IJob
  {
    public async Task Execute(IJobExecutionContext context)
    {
      SQLCommandJob cmd = (SQLCommandJob)context.JobDetail.JobDataMap.Get("sqlCommand");
      TraceSource trace = (TraceSource)context.JobDetail.JobDataMap.Get("trace");

      if (cmd == null || trace == null)
      {
        await Console.Error.WriteLineAsync("Refuz sa execut taskul din `RunSqlCommandJob` cu date ce lipsesc din context.");
        return;
      }

      trace.TraceInformation($"Se executa un task (Name='{cmd.Name}', SqlCommand='{cmd.SqlCommand}').");

      using (SqlConnection connection = new SqlConnection(cmd.ConnectionString))
      {
        try
        {
          await connection.OpenAsync();

          using (SqlCommand sqlCommand = connection.CreateCommand())
          {
            sqlCommand.CommandType = System.Data.CommandType.Text;
            sqlCommand.CommandText = cmd.SqlCommand;
            sqlCommand.CommandTimeout = cmd.CommandTimeout;

            int rowsAffected = await sqlCommand.ExecuteNonQueryAsync();
            trace.TraceInformation($"S-a executat taskul (Name='{cmd.Name}'). Randuri afectate = {rowsAffected}.");

            // using (SqlDataReader dataReader = await sqlCommand.ExecuteReaderAsync())
            // {
            //   while (await dataReader.ReadAsync())
            //   {
            //     for (int i = 0; i < dataReader.FieldCount; i++)
            //     {
            //       var value = dataReader.GetValue(i);
            //       trace.TraceInformation($"[SQLCommandJob.Name={cmd.Name}] Value={value}");
            //     }
            //   }
            // }
          }
        }
        catch (System.Exception e)
        {
          trace.TraceEvent(TraceEventType.Error, 0, $"Eroare executing un task (Name='{cmd.Name}'): {e.Message}");
        }
      }
    }
  }
}