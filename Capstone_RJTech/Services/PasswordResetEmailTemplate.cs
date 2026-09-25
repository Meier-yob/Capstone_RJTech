using System.Net;

namespace Capstone_RJTech.Services;

/// <summary>
/// Builds the HTML body of the password-reset verification email.
/// Table-based layout with inline styles only, so it renders consistently
/// in Gmail, Outlook and mobile mail clients.
/// </summary>
public static class PasswordResetEmailTemplate
{
    public const string Subject = "Your RJTech password reset code";

    private const string Template = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<meta name='color-scheme' content='light'>
<title>RJTech Verification Code</title>
</head>
<body style='margin:0;padding:0;background:#ffffff;'>
<div style='display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;'>Your RJTech verification code is {{OTP}}. It expires in {{MINUTES}} minutes.</div>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background:#ffffff;'>
  <tr>
    <td align='center' style='padding:16px;'>
      <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0' style='width:100%;max-width:600px;background:#f7f7f7;border:1px solid #d9d9d9;border-radius:16px;border-collapse:separate;overflow:hidden;'>
        <tr>
          <td bgcolor='#0066ff' style='background:#0066ff;padding:32px 30px;font-family:Arial,Helvetica,sans-serif;font-size:32px;line-height:1.2;font-weight:bold;color:#ffffff;'>
            RJTech Verification Code
          </td>
        </tr>
        <tr>
          <td style='padding:30px 30px 10px 30px;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:1.5;color:#1f1f1f;'>
            We received a request to use this email address to help recover your RJTech Account <span style='text-decoration:underline;'>{{EMAIL}}</span>.
          </td>
        </tr>
        <tr>
          <td align='center' style='padding:14px 30px;font-family:Arial,Helvetica,sans-serif;font-size:36px;line-height:1.2;font-weight:bold;letter-spacing:4px;color:#1f1f1f;'>
            {{OTP}}
          </td>
        </tr>
        <tr>
          <td style='padding:10px 30px 0 30px;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:1.5;color:#1f1f1f;'>
            Enter the code when asked for it on the verification page. <strong>It expires in {{MINUTES}} minutes.</strong>
          </td>
        </tr>
        <tr>
          <td style='padding:16px 30px 0 30px;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:1.5;color:#1f1f1f;'>
            If you did not recognize <span style='text-decoration:underline;'>{{EMAIL}}</span>, you can safely ignore this email.
          </td>
        </tr>
        <tr>
          <td style='padding:16px 30px 30px 30px;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:1.5;color:#1f1f1f;'>
            Sincerely,<br>The RJTech Accounts Team
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
</body>
</html>";

    public static string Build(string email, string otp, int expiresInMinutes)
    {
        return Template
            .Replace("{{EMAIL}}", WebUtility.HtmlEncode(email))
            .Replace("{{OTP}}", WebUtility.HtmlEncode(otp))
            .Replace("{{MINUTES}}", expiresInMinutes.ToString());
    }
}
