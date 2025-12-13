namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// HTML email templates with Codewrinkles branding.
///
/// Brand colors:
/// - Brand teal: #20C1AC
/// - Brand soft: #35D6C0
/// - Pulse accent: #38BDF8
///
/// Email uses light theme for compatibility with email clients.
/// </summary>
public static class EmailTemplates
{
    private const string BrandColor = "#20C1AC";
    private const string BrandColorSoft = "#35D6C0";
    private const string TextPrimary = "#0F172A";
    private const string TextSecondary = "#475569";
    private const string TextTertiary = "#94A3B8";
    private const string SurfaceCard = "#FFFFFF";
    private const string SurfacePage = "#F3F4F6";
    private const string Border = "#E2E8F0";

    public static string BuildWelcomeEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>Welcome to Codewrinkles</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with gradient -->
                                <tr>
                                    <td style="background: linear-gradient(135deg, {BrandColor} 0%, {BrandColorSoft} 100%); padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: white; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">
                                            Welcome to Codewrinkles!
                                        </h1>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style="background-color: {SurfaceCard}; padding: 40px 30px; border-radius: 0 0 16px 16px; border: 1px solid {Border}; border-top: none;">
                                        <p style="margin: 0 0 20px 0; font-size: 18px; color: {TextPrimary};">
                                            Hey {userName},
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            Thanks for joining Codewrinkles! We're excited to have you here.
                                        </p>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            Codewrinkles is where developers share ideas, learn together, and build their digital presence.
                                            Jump into <strong style="color: {TextPrimary};">Pulse</strong> to see what the community is talking about.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background: linear-gradient(135deg, {BrandColor} 0%, {BrandColorSoft} 100%);">
                                                    <a href="{baseUrl}/social" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: white; text-decoration: none; border-radius: 10px;">
                                                        Start Exploring
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 20px 0; font-size: 14px; color: {TextTertiary};">
                                            If you have any questions, just reply to this email. We're here to help!
                                        </p>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            Happy coding!<br>
                                            <strong style="color: {TextPrimary};">The Codewrinkles Team</strong>
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style="padding: 30px; text-align: center;">
                                        <p style="margin: 0; font-size: 12px; color: {TextTertiary};">
                                            &copy; 2025 Codewrinkles. All rights reserved.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    public static string BuildNotificationReminderEmail(string userName, int unreadCount, string baseUrl)
    {
        var notificationWord = unreadCount == 1 ? "notification" : "notifications";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>You have notifications on Pulse</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with gradient -->
                                <tr>
                                    <td style="background: linear-gradient(135deg, {BrandColor} 0%, {BrandColorSoft} 100%); padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: white; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
                                            You've got {notificationWord}!
                                        </h1>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style="background-color: {SurfaceCard}; padding: 40px 30px; border-radius: 0 0 16px 16px; border: 1px solid {Border}; border-top: none;">
                                        <p style="margin: 0 0 20px 0; font-size: 18px; color: {TextPrimary};">
                                            Hey {userName},
                                        </p>

                                        <p style="margin: 0 0 25px 0; font-size: 16px; color: {TextSecondary};">
                                            You've been away from Pulse for a bit, and some things happened while you were gone.
                                        </p>

                                        <!-- Stats Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-radius: 12px; padding: 30px; text-align: center;">
                                                    <div style="font-size: 52px; font-weight: 700; color: {BrandColor}; line-height: 1;">
                                                        {unreadCount}
                                                    </div>
                                                    <div style="font-size: 14px; color: {TextSecondary}; margin-top: 8px;">
                                                        unread {notificationWord}
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            People are engaging with your content &mdash; don't leave them hanging!
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background: linear-gradient(135deg, {BrandColor} 0%, {BrandColorSoft} 100%);">
                                                    <a href="{baseUrl}/social/notifications" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: white; text-decoration: none; border-radius: 10px;">
                                                        See What You Missed
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            See you on Pulse!<br>
                                            <strong style="color: {TextPrimary};">The Codewrinkles Team</strong>
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style="padding: 30px; text-align: center;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; color: {TextTertiary};">
                                            You're receiving this because you have unread notifications on Pulse.
                                        </p>
                                        <p style="margin: 0; font-size: 12px; color: {TextTertiary};">
                                            &copy; 2025 Codewrinkles. All rights reserved.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    public static string BuildFeedUpdateEmail(string userName, int newPulsesCount, string baseUrl)
    {
        var pulseWord = newPulsesCount == 1 ? "pulse" : "pulses";

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>Your feed is waiting on Pulse</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with gradient -->
                                <tr>
                                    <td style="background: linear-gradient(135deg, {BrandColor} 0%, {BrandColorSoft} 100%); padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: white; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
                                            Your feed is waiting!
                                        </h1>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style="background-color: {SurfaceCard}; padding: 40px 30px; border-radius: 0 0 16px 16px; border: 1px solid {Border}; border-top: none;">
                                        <p style="margin: 0 0 20px 0; font-size: 18px; color: {TextPrimary};">
                                            Hey {userName},
                                        </p>

                                        <p style="margin: 0 0 25px 0; font-size: 16px; color: {TextSecondary};">
                                            While you've been away, people you follow have been busy sharing ideas.
                                        </p>

                                        <!-- Stats Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-radius: 12px; padding: 30px; text-align: center;">
                                                    <div style="font-size: 52px; font-weight: 700; color: {BrandColor}; line-height: 1;">
                                                        {newPulsesCount}
                                                    </div>
                                                    <div style="font-size: 14px; color: {TextSecondary}; margin-top: 8px;">
                                                        new {pulseWord} in your feed
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            Catch up on what developers in your network are thinking about.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background: linear-gradient(135deg, {BrandColor} 0%, {BrandColorSoft} 100%);">
                                                    <a href="{baseUrl}/social" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: white; text-decoration: none; border-radius: 10px;">
                                                        See Your Feed
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            See you on Pulse!<br>
                                            <strong style="color: {TextPrimary};">The Codewrinkles Team</strong>
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style="padding: 30px; text-align: center;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; color: {TextTertiary};">
                                            You're receiving this because people you follow posted new content.
                                        </p>
                                        <p style="margin: 0; font-size: 12px; color: {TextTertiary};">
                                            &copy; 2025 Codewrinkles. All rights reserved.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
