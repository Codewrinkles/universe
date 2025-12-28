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

    public static string BuildSevenDayNovaWinbackEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>We miss you on Nova</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with Nova violet -->
                                <tr>
                                    <td style="background-color: {NovaColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
                                            We miss you on Nova!
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
                                            It's been about a week since your last conversation with Nova, and we wanted to check in.
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            Nova remembers your journey &mdash; your background, your goals, and where you left off. Whether you want to continue exploring a topic or start something new, your AI coach is ready when you are.
                                        </p>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            Come back and pick up where you left off!
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {NovaColor};">
                                                    <a href="{baseUrl}/nova" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Continue with Nova
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
                                            You're receiving this because you have Nova Alpha access.
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

    public static string BuildThirtyDayNovaWinbackEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>Important: Your Nova Alpha access</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with Nova violet -->
                                <tr>
                                    <td style="background-color: {NovaColor}; padding: 40px 30px; border-radius: 16px 16px 0 0; text-align: center;">
                                        <h1 style="margin: 0; color: #FFFFFF; font-size: 24px; font-weight: 700; letter-spacing: -0.5px;">
                                            We miss you on Nova
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
                                            It's been about a month since your last visit to Nova, and we wanted to reach out.
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            Nova still remembers everything &mdash; your background, your goals, and the conversations you've had. Your personalized AI coach is ready to pick up right where you left off.
                                        </p>

                                        <!-- Important Notice -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: #FEF3C7; border: 1px solid #F59E0B; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: #92400E;">
                                                        A note about Alpha access
                                                    </p>
                                                    <p style="margin: 0; font-size: 14px; color: #92400E;">
                                                        Nova is currently in Alpha, and for an Alpha to work well, it needs engaged users who can provide feedback and help us improve. To keep the Alpha community active, we may need to disable access for users who remain inactive for extended periods.
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            We'd love to have you back &mdash; even a quick conversation helps us understand how Nova can serve you better.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {NovaColor};">
                                                    <a href="{baseUrl}/nova" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Return to Nova
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
                                            You're receiving this because you have Nova Alpha access.
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

    public static string BuildSevenDayCodewrinklesWinbackEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>We miss you on Codewrinkles</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with brand teal -->
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
                                            It's been about a week since we saw you on Codewrinkles, and we wanted to check in.
                                        </p>

                                        <p style="margin: 0 0 20px 0; font-size: 16px; color: {TextSecondary};">
                                            Here's what you might be missing:
                                        </p>

                                        <!-- Pulse Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 16px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-left: 4px solid {BrandColor}; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 6px 0; font-size: 16px; font-weight: 600; color: {BrandColor};">
                                                        Pulse &mdash; Share Your Insights
                                                    </p>
                                                    <p style="margin: 0; font-size: 14px; color: {TextSecondary};">
                                                        Connect with developers, share what you're learning, and have genuine conversations. No algorithm, no engagement tricks &mdash; just a chronological feed.
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Nova Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-left: 4px solid {NovaColor}; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 6px 0; font-size: 16px; font-weight: 600; color: {NovaColor};">
                                                        Nova &mdash; Your AI Learning Coach
                                                    </p>
                                                    <p style="margin: 0; font-size: 14px; color: {TextSecondary};">
                                                        An AI coach that remembers your background, tracks your growth, and adapts every conversation to where you are in your journey. Currently in Alpha.
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            Come back and see what the community has been up to!
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Come Back to Codewrinkles
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
                                            You're receiving this because you haven't visited Codewrinkles in a while.
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

    public static string BuildThirtyDayCodewrinklesWinbackEmail(string userName, string baseUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <meta http-equiv="X-UA-Compatible" content="IE=edge">
                <title>Come back and discover Codewrinkles</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; background-color: {SurfacePage};">
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="background-color: {SurfacePage};">
                    <tr>
                        <td style="padding: 40px 20px;">
                            <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="600" style="margin: 0 auto; max-width: 600px;">

                                <!-- Header with brand teal -->
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
                                            Here's what you're missing:
                                        </p>

                                        <!-- Pulse Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 16px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-left: 4px solid {BrandColor}; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 6px 0; font-size: 16px; font-weight: 600; color: {BrandColor};">
                                                        Pulse &mdash; Share Your Insights
                                                    </p>
                                                    <p style="margin: 0; font-size: 14px; color: {TextSecondary};">
                                                        The community has been growing &mdash; developers sharing ideas, having conversations, and helping each other out. Your voice matters here.
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Nova Card -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: {SurfacePage}; border: 1px solid {Border}; border-left: 4px solid {NovaColor}; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 6px 0; font-size: 16px; font-weight: 600; color: {NovaColor};">
                                                        Nova &mdash; Your AI Learning Coach
                                                    </p>
                                                    <p style="margin: 0; font-size: 14px; color: {TextSecondary};">
                                                        An AI coach that remembers your background, tracks your growth, and adapts every conversation to where you are in your journey. Currently in Alpha.
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>

                                        <!-- Nova Access Info -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" width="100%" style="margin-bottom: 25px;">
                                            <tr>
                                                <td style="background-color: #EDE9FE; border: 1px solid {NovaColorSoft}; border-radius: 8px; padding: 16px 20px;">
                                                    <p style="margin: 0 0 8px 0; font-size: 14px; font-weight: 600; color: {NovaColor};">
                                                        Want to try Nova?
                                                    </p>
                                                    <p style="margin: 0; font-size: 14px; color: #5B21B6;">
                                                        You can <a href="{baseUrl}/alpha/apply" style="color: {NovaColor}; font-weight: 500;">apply for Alpha access</a>, or earn it automatically by posting 15+ Pulses in 30 days. Show us you're serious about growth!
                                                    </p>
                                                </td>
                                            </tr>
                                        </table>

                                        <p style="margin: 0 0 30px 0; font-size: 16px; color: {TextSecondary};">
                                            We'd love to have you back.
                                        </p>

                                        <!-- CTA Button -->
                                        <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin: 0 auto 30px auto;">
                                            <tr>
                                                <td style="border-radius: 10px; background-color: {BrandColor};">
                                                    <a href="{baseUrl}/" target="_blank" style="display: inline-block; padding: 16px 36px; font-size: 16px; font-weight: 600; color: #FFFFFF; text-decoration: none; border-radius: 10px;">
                                                        Rejoin Codewrinkles
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
                                            You're receiving this because you haven't visited Codewrinkles in a while.
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
                                            You're In!
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
                                            You Earned It!
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
