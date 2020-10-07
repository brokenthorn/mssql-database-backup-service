namespace BtSqlBackupService
{
  public static class Constants
  {
    /// <summary>
    ///   Windows service name. Needs to be unique per system.
    /// </summary>
    public const string serviceName = "bt_sql_backup_service";

    /// <summary>
    ///   Windows service display name.
    /// </summary>
    public const string displayName = "SQL Backup Service";

    /// <summary>
    ///   Windows service description field.
    /// </summary>
    public const string description = "BrokenThorn's SQL Backup Service";
  }
}