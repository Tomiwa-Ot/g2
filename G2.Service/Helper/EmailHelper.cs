using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace G2.Service.Helper
{
    public enum EmailType
    {
        account_locked,
        forgot_password,
        email_verification,
        password_reset
    }

    public class EmailHelper
    {
        private readonly IConfiguration _configuration;
        private readonly string _username;
        private readonly string _password;

        public EmailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            _username = _configuration.GetSection("Mail")["UserName"];
            _password = _configuration.GetSection("Mail")["Password"];
        }

        public void SendMail(EmailType type, string to, dynamic? body)
        {
            string mailBody = "";
            string title = "";
            switch (type)
            {
                case EmailType.account_locked:
                    mailBody = EmailBody.AccountLocked;
                    mailBody = mailBody.Replace("Duration", body.Duration);
                    title = "Account Locked";
                    break;
                case EmailType.forgot_password:
                    mailBody = EmailBody.ForgotPassword;
                    mailBody = mailBody.Replace("ResetToken", body.ResetToken);
                    mailBody = mailBody.Replace("Expiration", body.Expiration);
                    title = "Password Reset";
                    break;
                case EmailType.email_verification:
                    mailBody = EmailBody.EmailVerification;
                    mailBody = mailBody.Replace("Token", body.Token);
                    title = "Account Verification";
                    break;
                case EmailType.password_reset:
                    mailBody = EmailBody.PasswordReset;
                    mailBody = mailBody.Replace("UpdatedAt", body.UpdatedAt);
                    title = "Password Reset Successful";
                    break;
            }

            mailBody = mailBody.Replace("Fullname", body.Fullname);

            MimeMessage email = new MimeMessage();
            email.From.Add(new MailboxAddress("Support Team", "info@g2.sh"));
            email.To.Add(new MailboxAddress(body.Fullname ?? to, to));
            email.Subject = title;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { 
                Text = mailBody
            };

            using var smtp = new SmtpClient();
            smtp.Connect("smtp.gmail.com", 465, true);

            // Note: only needed if the SMTP server requires authentication
            smtp.Authenticate(_username, _password);
            smtp.Send(email);
            smtp.Disconnect(true);
        }
    }
}