using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MortuaryApp.Data;

namespace MortuaryApp.Services;

public class EmailService
{
    public static async Task SendAsync(string to, string subject, string body)
    {
        using var db = new MortuaryDbContext();
        var host = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpHost"))?.Value ?? "";
        var portStr = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpPort"))?.Value ?? "587";
        var username = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpUsername"))?.Value ?? "";
        var password = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpPassword"))?.Value ?? "";
        var from = (await db.Settings.FirstOrDefaultAsync(s => s.Key == "SmtpFromEmail"))?.Value ?? "";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("SMTP host and from email must be configured in Settings.");

        if (!int.TryParse(portStr, out var port))
            port = 587;

        using var client = new SmtpClient(host, port);
        client.EnableSsl = true;
        client.UseDefaultCredentials = false;

        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password ?? "");

        var message = new MailMessage(from, to, subject, body);
        await client.SendMailAsync(message);
    }
}
