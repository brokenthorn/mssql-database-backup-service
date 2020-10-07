namespace BtSqlBackupService.Entities
{
  /// <summary>
  ///   Settings for the SMTP mail client.
  /// </summary>
  public class SmtpClientSettings
  {
    public string Server { get; set; }
    public int Port { get; set; }
    public string SenderName { get; set; }
    public string SenderEmail { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string AdminEmail { get; set; }
  }
}