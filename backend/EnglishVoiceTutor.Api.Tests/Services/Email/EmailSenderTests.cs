using System.Net.Mail;
using System.Reflection;
using EnglishVoiceTutor.Api.Contracts.Auth;
using EnglishVoiceTutor.Api.Data;
using EnglishVoiceTutor.Api.Data.Entities;
using EnglishVoiceTutor.Api.Options;
using EnglishVoiceTutor.Api.Services.Auth;
using EnglishVoiceTutor.Api.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EnglishVoiceTutor.Api.Tests.Services.Email;

public sealed class EmailSenderTests
{
    [Fact]
    public void GenericMessageMapsPlainTextAndHtmlWithoutCallerControlledFrom()
    {
        using var plainText = CreateMailMessage(new EmailMessage("recipient@example.test", "Subject", "Plain text"));
        using var html = CreateMailMessage(new EmailMessage("recipient@example.test", "Subject", "Plain alternative", "<p>HTML</p>"));

        Assert.Equal("recipient@example.test", plainText.To.Single().Address);
        Assert.Equal("Subject", plainText.Subject);
        Assert.Equal("Plain text", plainText.Body);
        Assert.False(plainText.IsBodyHtml);
        Assert.Equal("sender@example.test", plainText.From!.Address);
        Assert.Equal("Configured Sender", plainText.From.DisplayName);
        Assert.True(html.IsBodyHtml);
        Assert.Equal("<p>HTML</p>", html.Body);
        Assert.Equal("sender@example.test", html.From!.Address);
    }

    [Theory]
    [InlineData("", "subject", "body")]
    [InlineData("recipient@example.test", "", "body")]
    [InlineData("recipient@example.test", "subject", "")]
    public void GenericMessageRejectsBlankRequiredFields(string recipient, string subject, string body)
    {
        Assert.Throws<EmailMessageValidationException>(() => new EmailMessage(recipient, subject, body));
    }

    [Fact]
    public async Task UnconfiguredSenderReportsNotConfiguredWithoutTransportAttempt()
    {
        var sender = new NoOpEmailSender(NullLogger<NoOpEmailSender>.Instance);

        await sender.SendAsync(new EmailMessage("recipient@example.test", "Subject", "Body"), TestContext.Current.CancellationToken);

        Assert.False(sender.IsConfigured);
    }

    [Fact]
    public void SmtpSelectionRequiresEnabledAndOtherwiseValidConfiguration()
    {
        var valid = new SmtpEmailOptions
        {
            Enabled = true,
            Host = "smtp.example.test",
            Port = 587,
            FromAddress = "sender@example.test",
            FromName = "Configured Sender",
            UserName = "smtp-user",
            Password = "smtp-password"
        };

        Assert.True(EmailSenderSelection.ShouldUseSmtp(valid));
        Assert.False(EmailSenderSelection.ShouldUseSmtp(new SmtpEmailOptions
        {
            Enabled = false,
            Host = valid.Host,
            Port = valid.Port,
            FromAddress = valid.FromAddress,
            FromName = valid.FromName,
            UserName = valid.UserName,
            Password = valid.Password
        }));
        Assert.False(EmailSenderSelection.ShouldUseSmtp(new SmtpEmailOptions { Enabled = true, Port = valid.Port, FromAddress = valid.FromAddress }));
    }

    [Fact]
    public void DisabledSmtpSelectionUsesUnconfiguredNoOpSenderWithoutTransport()
    {
        var options = new SmtpEmailOptions
        {
            Enabled = false,
            Host = "smtp.example.test",
            Port = 587,
            FromAddress = "sender@example.test",
            FromName = "Configured Sender",
            UserName = "smtp-user",
            Password = "smtp-password"
        };

        var sender = EmailSenderSelection.ShouldUseSmtp(options)
            ? throw new InvalidOperationException("Disabled SMTP must not select the transport.")
            : new NoOpEmailSender(NullLogger<NoOpEmailSender>.Instance);

        Assert.False(sender.IsConfigured);
    }

    [Fact]
    public async Task PasswordResetAdapterPreservesSubjectAndPlainTextBody()
    {
        var capture = new CapturingEmailSender();
        var adapter = new PasswordResetEmailSender(capture);
        var user = new UserEntity { Id = Guid.NewGuid(), Email = "learner@example.test" };

        await adapter.SendPasswordResetAsync(user, "123456", "https://example.test/reset?code=123456", TestContext.Current.CancellationToken);

        Assert.NotNull(capture.Message);
        Assert.Equal("Reset your Language Voice Tutor password", capture.Message.Subject);
        Assert.Null(capture.Message.HtmlBody);
        Assert.Equal(
            "We received a request to reset your Language Voice Tutor password.\n\nOpen this reset link or enter the reset code in Language Voice Tutor Desktop.\n\nReset link: https://example.test/reset?code=123456\n\nReset code: 123456\n\nThis code expires soon and can be used only once. If you did not request this reset, you can ignore this email.",
            capture.Message.PlainTextBody);
    }

    [Fact]
    public async Task PasswordResetRequireConfiguredAndDeliveryFailureBehaviorRemainUnchanged()
    {
        await using var db = CreateDbContext();
        var unavailableService = CreatePasswordResetService(db, new FakePasswordResetSender { IsConfiguredValue = false }, requireConfiguredSender: true);

        await Assert.ThrowsAsync<PasswordResetDeliveryUnavailableException>(() => unavailableService.RequestPasswordResetAsync(
            new PasswordResetRequest { Email = "missing@example.test" }, TestContext.Current.CancellationToken));

        var user = new UserEntity { Id = Guid.NewGuid(), Email = "learner@example.test", PasswordHash = "hash", Status = "active", CreatedAt = DateTimeOffset.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        var failingService = CreatePasswordResetService(db, new FakePasswordResetSender { IsConfiguredValue = true, ThrowOnSend = true }, requireConfiguredSender: true);

        await Assert.ThrowsAsync<PasswordResetDeliveryUnavailableException>(() => failingService.RequestPasswordResetAsync(
            new PasswordResetRequest { Email = user.Email }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void OnlyGenericSmtpSenderOwnsSmtpClientConstruction()
    {
        var repositoryRoot = FindRepositoryRoot();
        var emailSources = Directory.GetFiles(Path.Combine(repositoryRoot, "backend", "EnglishVoiceTutor.Api", "Services", "Email"), "*.cs");
        var smtpClientUseCount = emailSources.Sum(path => CountOccurrences(File.ReadAllText(path), "new SmtpClient("));

        Assert.Equal(1, smtpClientUseCount);
        Assert.DoesNotContain("SmtpClient", File.ReadAllText(Path.Combine(repositoryRoot, "backend", "EnglishVoiceTutor.Api", "Services", "Email", "PasswordResetEmailSender.cs")), StringComparison.Ordinal);
    }

    private static MailMessage CreateMailMessage(EmailMessage message)
    {
        var method = typeof(SmtpEmailSender).GetMethod("CreateMailMessage", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<MailMessage>(method.Invoke(null, [message, new SmtpEmailOptions
        {
            Host = "smtp.example.test", Port = 587, FromAddress = "sender@example.test", FromName = "Configured Sender"
        }]));
    }

    private static PasswordResetService CreatePasswordResetService(AppDbContext db, IPasswordResetEmailSender sender, bool requireConfiguredSender) => new(
        db,
        new PasswordHasher<UserEntity>(),
        sender,
        Microsoft.Extensions.Options.Options.Create(new PasswordResetOptions { Enabled = true, RequireConfiguredEmailSender = requireConfiguredSender }),
        NullLogger<PasswordResetService>.Instance);

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "backend", "EnglishVoiceTutor.Api")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public bool IsConfigured => true;
        public EmailMessage? Message { get; private set; }
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Message = message;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordResetSender : IPasswordResetEmailSender
    {
        public bool IsConfiguredValue { get; init; }
        public bool ThrowOnSend { get; init; }
        public bool IsConfigured => IsConfiguredValue;
        public Task SendPasswordResetAsync(UserEntity user, string resetCode, string resetUrl, CancellationToken cancellationToken)
        {
            if (ThrowOnSend) throw new EmailDeliveryException();
            return Task.CompletedTask;
        }
    }
}
