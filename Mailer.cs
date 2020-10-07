using System;
using System.Threading.Tasks;
using BtSqlBackupService.Entities;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace BtSqlBackupService
{
  /// <summary>
  ///   A simple electronic mail sender.
  /// </summary>
  public class Mailer
  {
    private readonly ILogger _logger;
    private readonly SmtpClientSettings _smtpClientSettings;

    public Mailer(SmtpClientSettings smtpClientSettings, ILogger logger)
    {
      _smtpClientSettings = smtpClientSettings;
      _logger = logger;
    }

    /// <summary>
    ///   Sends an email message with an HTML body.
    /// </summary>
    /// <param name="toEmailAddress">The email address of the receiver</param>
    /// <param name="subject">The email's subject</param>
    /// <param name="body">The HTML or text body of the emial</param>
    /// <returns>A task for the completion of the command.</returns>
    /// <exception cref="InvalidOperationException">
    ///   Thrown when the underlying implementation throws any errors.
    /// </exception>
    public async Task SendEmailAsync(string toEmailAddress, string subject,
      string body)
    {
      try
      {
        _logger.LogInformation(
          $"Sending email to {toEmailAddress}, subject '{subject}'.");

        var message = new MimeMessage();

        message.Sender = new MailboxAddress(_smtpClientSettings.SenderName,
          _smtpClientSettings.SenderEmail);

        foreach (var address in toEmailAddress.Split(';'))
        {
          message.To.Add(MailboxAddress.Parse(address));
        }

        message.Subject = subject;
        message.Body = new TextPart("html") {Text = body};

        using (var client = new SmtpClient())
        {
          client.ServerCertificateValidationCallback = (s, c, h, e) => true;

          await client.ConnectAsync(_smtpClientSettings.Server,
            _smtpClientSettings.Port);
          await client.AuthenticateAsync(_smtpClientSettings.Username,
            _smtpClientSettings.Password);
          await client.SendAsync(message);
          await client.DisconnectAsync(true);
          
          _logger.LogInformation(
            $"Email sent to {toEmailAddress}, subject '{subject}'.");
        }
      }
      catch (Exception e)
      {
        _logger.LogError(
          $"Failed to send email to {toEmailAddress}: {e.Message}");
      }
    }
  }
}