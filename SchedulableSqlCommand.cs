namespace bt_sql_backup_service
{
  /// <summary>
  /// Model class for a schedulable SQL command.
  /// </summary>
  public class SchedulableSQLCommand
  {
    /// <summary>
    /// A short name for this command.
    /// </summary>
    /// <value></value>
    public string Name { get; set; }

    /// <summary>
    /// A description of what this command is or does.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Connection string for the SQL server that the command will be executed on.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// SQL command to execute.
    /// </summary>
    public string SqlCommand { get; set; }

    /// <summary>
    /// The number of seconds to wait before terminating the attempt to execute
    /// the SqlCommand and returning an error.
    /// </summary>
    /// <value>Timeout in seconds</value>
    public int CommandTimeout { get; set; }

    /// <summary>
    /// Schedule in cron syntax style.
    /// </summary>
    public string Cron { get; set; }
  }
}