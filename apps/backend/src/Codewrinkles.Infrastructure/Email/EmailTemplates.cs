namespace Codewrinkles.Infrastructure.Email;

/// <summary>
/// HTML email templates with Codewrinkles branding.
///
/// Brand colors:
/// - Brand teal: #20C1AC
/// - Brand soft: #35D6C0
/// - Pulse accent: #38BDF8
/// - Nova violet: #8B5CF6
/// - Nova violet soft: #A78BFA
///
/// Email uses light theme for compatibility with email clients.
/// </summary>
public static class EmailTemplates
{
    private const string BrandColor = "#20C1AC";
    private const string BrandColorSoft = "#35D6C0";
    private const string NovaColor = "#8B5CF6";
    private const string NovaColorSoft = "#A78BFA";
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

                                <!-- Header with solid background (gradient fallback) -->
                                <tr>
                                    <td style="background-color: {BrandColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">
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
                                            Thanks for joining! Codewrinkles is an ecosystem built to help developers grow. Here's what you now have access to:
                                        </p>

                                        <!-- Nova Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 16px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-left: 4px solid {NovaColor}; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 6px 0; font-size: 16px; font-weight: 600; color: {NovaColor};">
                                                        Nova &mdash; AI Learning Coach
                                                    </p>
                                                    <p style="margin: 0 0 10px 0; font-size: 14px; color: {TextSecondary};">
                                                        An AI coach that remembers your background, tracks your growth, and adapts every conversation to where you are in your journey.
                                                    </p>
                                                    <a href="{baseUrl}/nova" target="_blank" style="font-size: 14px; font-weight: 500; color: {NovaColor}; text-decoration: none;">
                                                        Try Nova &rarr;
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Pulse Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 16px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-left: 4px solid {BrandColor}; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 6px 0; font-size: 16px; font-weight: 600; color: {BrandColor};">
                                                        Pulse &mdash; Developer Community
                                                    </p>
                                                    <p style="margin: 0 0 10px 0; font-size: 14px; color: {TextSecondary};">
                                                        A place where your followers actually see your posts. No algorithm, no engagement tricks &mdash; just a chronological feed and genuine conversations.
                                                    </p>
                                                    <a href="{baseUrl}/pulse" target="_blank" style="font-size: 14px; font-weight: 500; color: {BrandColor}; text-decoration: none;">
                                                        Explore Pulse &rarr;
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- YouTube Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-left: 4px solid #EF4444; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 6px 0; font-size: 16px; font-weight: 600; color: #EF4444;">
                                                        YouTube &mdash; Deep Dives
                                                    </p>
                                                    <p style="margin: 0 0 10px 0; font-size: 14px; color: {TextSecondary};">
                                                        Long-form content on architecture, patterns, and real-world .NET development. The content that started it all.
                                                    </p>
                                                    <a href="https://www.youtube.com/@Codewrinkles" target="_blank" style="font-size: 14px; font-weight: 500; color: #EF4444; text-decoration: none;">
                                                        Watch on YouTube &rarr;
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            We believe content should be discovered by its value, not by virality metrics. Welcome to a community that thinks the same way.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/pulse" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Start Exploring
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 20px 0; font-size: 14px; color: {TextTertiary};">
                                            If you have any questions, just reply to this email. We're here to help!
                                        </p>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            See you around!<br>
                                            <strong style="color: {TextPrimary};">Dan &amp; the Codewrinkles Team</strong>
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

                                <!-- Header with solid background (gradient fallback) -->
                                <tr>
                                    <td style="background-color: {BrandColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
                                            You've got notifications!
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
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/pulse/notifications" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
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

                                <!-- Header with solid background (gradient fallback) -->
                                <tr>
                                    <td style="background-color: {BrandColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
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
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/pulse" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
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

    public static string BuildSevenDayWinbackEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>We miss you on Pulse</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with solid background (gradient fallback) -->
                                <tr>
                                    <td style="background-color: {BrandColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
                                            We miss you!
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
                                            It's been about a week since we saw you on Pulse, and we wanted to check in.
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            The community has been active &mdash; developers sharing ideas, having conversations, and building connections. Your voice matters here.
                                        </p>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            Come back and see what you've been missing!
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/pulse" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Come Back to Pulse
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            See you soon!<br>
                                            <strong style="color: {TextPrimary};">The Codewrinkles Team</strong>
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style="padding: 30px; text-align: center;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; color: {TextTertiary};">
                                            You're receiving this because you haven't visited Pulse in a while.
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

    public static string BuildThirtyDayWinbackEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>It's been a while</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with solid background (gradient fallback) -->
                                <tr>
                                    <td style="background-color: {BrandColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
                                            It's been a while...
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
                                            A month is a long time! We noticed you haven't been around, and we wanted to reach out.
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            The Pulse community has been growing &mdash; new conversations, fresh perspectives, and developers helping each other out. It's not the same without you.
                                        </p>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            We'd love to have you back.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/pulse" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Rejoin the Conversation
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            Hope to see you soon!<br>
                                            <strong style="color: {TextPrimary};">The Codewrinkles Team</strong>
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style="padding: 30px; text-align: center;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; color: {TextTertiary};">
                                            You're receiving this because you haven't visited Pulse in a while.
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

    public static string BuildAlphaAcceptanceEmail(string userName, string inviteCode, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>You're In! Welcome to Nova Alpha</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with Nova violet -->
                                <tr>
                                    <td style="background-color: {NovaColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">
                                            You're In! 🎉
                                        </h1>
                                        <p style="margin: 10px 0 0 0; color: rgba(255,255,255,0.9); font-size: 16px;">
                                            Welcome to the Nova Alpha
                                        </p>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style="background-color: {SurfaceCard}; padding: 40px 30px; border-radius: 0 0 16px 16px; border: 1px solid {Border}; border-top: none;">
                                        <p style="margin: 0 0 20px 0; font-size: 18px; color: {TextPrimary};">
                                            Hey {userName},
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            Great news &mdash; your application to join the Nova Alpha has been <strong style="color: {NovaColor};">accepted</strong>!
                                        </p>

                                        <p style="margin: 0 0 25px 0; font-size: 16px; color: {TextSecondary};">
                                            You're now one of only 50 developers who will shape the future of AI-powered learning. As a founding member, you'll get free unlimited access during Alpha and a lifetime discount when we launch.
                                        </p>

                                        <!-- Invite Code Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 2px solid {NovaColor}; border-radius: 12px; padding: 24px; text-align: center;">
                                                    <div style="font-size: 12px; color: {TextTertiary}; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 8px;">
                                                        Your Invite Code
                                                    </div>
                                                    <div style="font-size: 32px; font-weight: 700; color: {NovaColor}; font-family: 'SF Mono', Monaco, 'Courier New', monospace; letter-spacing: 2px;">
                                                        {inviteCode}
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            Use this code to unlock Nova access. Click below to redeem it now!
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {NovaColor};">
                                                    <a href="{baseUrl}/nova/redeem" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Redeem Your Code
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 20px 0; font-size: 14px; color: {TextTertiary};">
                                            Remember: as an Alpha tester, we ask you to complete your learning profile within 24 hours, have at least 5 conversations in 2 weeks, and share honest feedback.
                                        </p>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            Welcome to the team!<br>
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

    public static string BuildPulseAlphaEarnedEmail(string userName, int pulseCount, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>You Earned Nova Alpha Access!</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with Nova violet -->
                                <tr>
                                    <td style="background-color: {NovaColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">
                                            You Earned It! 🏆
                                        </h1>
                                        <p style="margin: 10px 0 0 0; color: rgba(255,255,255,0.9); font-size: 16px;">
                                            Nova Alpha Access Unlocked
                                        </p>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style="background-color: {SurfaceCard}; padding: 40px 30px; border-radius: 0 0 16px 16px; border: 1px solid {Border}; border-top: none;">
                                        <p style="margin: 0 0 20px 0; font-size: 18px; color: {TextPrimary};">
                                            Hey {userName},
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            Your activity on Codewrinkles Pulse has been noticed!
                                        </p>

                                        <!-- Stats Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 2px solid {NovaColor}; border-radius: 12px; padding: 24px; text-align: center;">
                                                    <div style="font-size: 52px; font-weight: 700; color: {NovaColor}; line-height: 1;">
                                                        {pulseCount}
                                                    </div>
                                                    <div style="font-size: 14px; color: {TextSecondary}; margin-top: 8px;">
                                                        pulses in the last 30 days
                                                    </div>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            That dedication has unlocked <strong style="color: {NovaColor};">Nova Alpha access</strong>! You're now one of only 50 developers shaping the future of AI-powered learning.
                                        </p>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            As a founding member, you get free unlimited access during Alpha and a lifetime discount when we launch.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {NovaColor};">
                                                    <a href="{baseUrl}/nova" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Start Using Nova
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 20px 0; font-size: 14px; color: {TextTertiary};">
                                            No invite code needed &mdash; your access is already active. Just log in and go to Nova!
                                        </p>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            Thanks for being an active part of Codewrinkles!<br>
                                            <strong style="color: {TextPrimary};">The Codewrinkles Team</strong>
                                        </p>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style="padding: 30px; text-align: center;">
                                        <p style="margin: 0 0 5px 0; font-size: 12px; color: {TextTertiary};">
                                            You're receiving this because you qualified for Nova Alpha through your Pulse activity.
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

    public static string BuildAlphaWaitlistEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>You're on the Nova Waitlist</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with Nova violet (softer) -->
                                <tr>
                                    <td style="background-color: {NovaColorSoft}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 28px; font-weight: 700; letter-spacing: -0.5px;">
                                            You're on the Waitlist
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
                                            Thanks for applying to the Nova Alpha! We've reviewed your application and added you to our <strong>priority waitlist</strong>.
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            We're starting small with just 50 testers to ensure we can give everyone personalized attention and act on feedback quickly. As spots open up (or as we expand), you'll be among the first to get access.
                                        </p>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            In the meantime, you can explore <strong>Pulse</strong> &mdash; our developer community where you can connect with other developers who are waiting alongside you.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/pulse" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Explore Pulse
                                                    </a>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 20px 0; font-size: 14px; color: {TextTertiary};">
                                            We'll notify you as soon as a spot opens up. Thanks for your patience!
                                        </p>

                                        <p style="margin: 0; font-size: 16px; color: {TextSecondary};">
                                            Talk soon!<br>
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
}
