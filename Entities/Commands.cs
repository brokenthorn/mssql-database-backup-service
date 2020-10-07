namespace BtSqlBackupService.Entities
{
  /// <summary>
  ///   A SQL command entity.
  /// </summary>
  /// <remarks>
  ///   This entity holds information about a SQL command script, a Cron schedule
  ///   and SQL Server connection string representing the server on which the
  ///   command should be executed.
  /// </remarks>
  public class SqlCommandEntity
  {
    /// <summary>
    ///   A short name for this command.
    /// </summary>
    /// <value></value>
    public string Name { get; set; }

    /// <summary>
    ///   A description of what this command is or does.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    ///   Connection string for the SQL server that the command will be executed on.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    ///   SQL command to execute.
    /// </summary>
    public string SqlCommand { get; set; }

    /// <summary>
    ///   The number of seconds to wait before terminating the attempt to execute
    ///   the SqlCommand and returning an error.
    /// </summary>
    /// <value>Timeout in seconds</value>
    public int CommandTimeout { get; set; }

    /// <summary>
    ///   Schedule in cron syntax style.
    /// </summary>
    public string Cron { get; set; }

    /// <summary>
    ///   An email address or comma separated list of email addresses to send
    ///   notifications to.
    /// </summary>
    /// <value></value>
    public string NotifyEmails { get; set; }
  }
}