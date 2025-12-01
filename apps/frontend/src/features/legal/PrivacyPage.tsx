import { Link } from "react-router-dom";

export function PrivacyPage(): JSX.Element {
  return (
    <div className="min-h-screen bg-surface-page py-12">
      <div className="mx-auto max-w-4xl px-4">
        {/* Header */}
        <div className="mb-8">
          <Link to="/" className="inline-flex items-center gap-2 text-sm text-text-secondary hover:text-text-primary mb-6">
            <span>←</span>
            <span>Back to Home</span>
          </Link>
          <h1 className="text-3xl font-bold tracking-tight text-text-primary mb-2">
            Privacy Policy
          </h1>
          <p className="text-sm text-text-secondary">
            Last updated: December 1, 2024
          </p>
        </div>

        {/* Content */}
        <div className="prose prose-invert max-w-none space-y-8 text-text-secondary">
          {/* Quick Summary */}
          <section className="p-4 bg-brand/10 border border-brand/30 rounded-lg">
            <h2 className="text-lg font-semibold text-brand-soft mb-3">🔒 Privacy at a Glance</h2>
            <ul className="space-y-2 text-sm">
              <li>✅ <strong>No Cookie Banner Needed:</strong> We don't use cookies at all</li>
              <li>✅ <strong>No Tracking:</strong> Zero analytics, advertising, or third-party trackers</li>
              <li>✅ <strong>No Data Selling:</strong> We never sell your personal information</li>
              <li>✅ <strong>Local Storage Only:</strong> We use browser local storage for authentication and preferences (strictly necessary)</li>
              <li>✅ <strong>Transparent:</strong> Everything we collect and why is explained below</li>
            </ul>
          </section>

          {/* Introduction */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">1. Introduction</h2>
            <p className="leading-relaxed">
              Codewrinkles ("we," "our," or "us") is committed to protecting your privacy. This Privacy Policy explains how we collect, use, disclose, and safeguard your information when you use our Service.
            </p>
            <p className="leading-relaxed mt-3">
              We build in public and value transparency. This policy reflects our commitment to honest, straightforward communication about how we handle your data.
            </p>
          </section>

          {/* Information We Collect */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">2. Information We Collect</h2>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">Information You Provide</h3>
            <p className="leading-relaxed">
              When you register and use our Service, we collect information you voluntarily provide:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li><strong>Account Information:</strong> Name, email address, handle/username, password (encrypted)</li>
              <li><strong>Profile Information:</strong> Bio, profile picture, and other optional profile details</li>
              <li><strong>Content:</strong> Posts (Pulses), comments, likes, bookmarks, and other interactions</li>
              <li><strong>Communications:</strong> Messages you send to us or other users through the Service</li>
            </ul>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">Automatically Collected Information</h3>
            <p className="leading-relaxed">
              When you access our Service, we automatically collect:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li><strong>Log Data:</strong> IP address, browser type, operating system, access times, pages viewed</li>
              <li><strong>Device Information:</strong> Device type, unique device identifiers</li>
              <li><strong>Usage Data:</strong> How you interact with the Service, features used, actions taken</li>
            </ul>
          </section>

          {/* Local Storage */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">2.1. Browser Local Storage</h2>
            <p className="leading-relaxed">
              <strong>Important:</strong> We do not use cookies. Instead, we use browser local storage to store essential data on your device.
            </p>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">What We Store Locally</h3>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li><strong>Authentication Tokens:</strong> Access and refresh tokens to keep you logged in (strictly necessary for the Service to function)</li>
              <li><strong>User Profile Data:</strong> Your name, handle, and profile information for quick access</li>
              <li><strong>Theme Preference:</strong> Your dark/light mode preference</li>
            </ul>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">What We Don't Store</h3>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>❌ <strong>No HTTP Cookies:</strong> We do not set any browser cookies</li>
              <li>❌ <strong>No Tracking Cookies:</strong> We do not use analytics or advertising cookies</li>
              <li>❌ <strong>No Third-Party Trackers:</strong> No Google Analytics, Facebook Pixel, or similar tracking technologies</li>
              <li>❌ <strong>No Cross-Site Tracking:</strong> We do not track you across other websites</li>
            </ul>

            <p className="leading-relaxed mt-3">
              <strong>Managing Local Storage:</strong> You can clear local storage at any time through your browser settings. Note that clearing local storage will log you out and reset your preferences.
            </p>

            <p className="leading-relaxed mt-3 p-3 bg-surface-card1 rounded-lg border border-border">
              <strong>Privacy First:</strong> Because we only use strictly necessary local storage (required for authentication and basic functionality), <strong>no cookie consent banner is required</strong> under GDPR, CCPA, or other privacy regulations. We believe in transparency over annoying pop-ups.
            </p>
          </section>

          {/* How We Use Your Information */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">3. How We Use Your Information</h2>
            <p className="leading-relaxed">
              We use your information to:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>Provide, operate, and maintain the Service</li>
              <li>Create and manage your account</li>
              <li>Process your transactions and deliver content</li>
              <li>Send you important updates about the Service</li>
              <li>Respond to your comments, questions, and support requests</li>
              <li>Improve and personalize your experience</li>
              <li>Monitor usage and analyze trends to improve the Service</li>
              <li>Detect, prevent, and address technical issues and security threats</li>
              <li>Enforce our Terms of Service and protect our rights</li>
            </ul>
            <p className="leading-relaxed mt-3">
              <strong>What We Don't Do:</strong> We do not sell your personal information. We do not use algorithmic feeds to manipulate engagement. We do not share your data with third-party advertisers. We do not use tracking cookies or analytics to profile you.
            </p>
          </section>

          {/* OAuth Third-Party Authentication */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">3.1. OAuth Authentication (Google & GitHub)</h2>
            <p className="leading-relaxed">
              When you sign in using Google or GitHub OAuth:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li><strong>What We Receive:</strong> Your email address, name, and profile picture from the OAuth provider</li>
              <li><strong>What We Don't Receive:</strong> We do not receive your Google/GitHub password or access to your Google/GitHub account beyond basic profile information</li>
              <li><strong>Provider Privacy Policies:</strong> Your use of Google/GitHub OAuth is subject to their respective privacy policies</li>
              <li><strong>Revocation:</strong> You can revoke Codewrinkles' access to your OAuth account at any time through your Google/GitHub account settings</li>
            </ul>
            <p className="leading-relaxed mt-3">
              We use OAuth solely for authentication convenience. We do not track your activity on Google or GitHub, and we do not share your Codewrinkles data with these providers beyond what is necessary for authentication.
            </p>
          </section>

          {/* Information Sharing */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">4. How We Share Your Information</h2>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">Public Information</h3>
            <p className="leading-relaxed">
              Content you post publicly (Pulses, profile information, etc.) is visible to other users and may be searchable. Be mindful of what you choose to share publicly.
            </p>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">Service Providers</h3>
            <p className="leading-relaxed">
              We may share your information with third-party service providers who help us operate the Service, such as:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>Cloud hosting providers (for infrastructure)</li>
              <li>Analytics providers (to understand usage patterns)</li>
              <li>Email service providers (for transactional emails)</li>
            </ul>
            <p className="leading-relaxed mt-3">
              These providers are bound by confidentiality agreements and may only use your data as necessary to perform services on our behalf.
            </p>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">Legal Requirements</h3>
            <p className="leading-relaxed">
              We may disclose your information if required to do so by law or in response to valid requests by public authorities (e.g., court orders, subpoenas).
            </p>

            <h3 className="text-lg font-semibold text-text-primary mt-4 mb-2">Business Transfers</h3>
            <p className="leading-relaxed">
              If we are involved in a merger, acquisition, or sale of assets, your information may be transferred. We will notify you before your information becomes subject to a different privacy policy.
            </p>
          </section>

          {/* Data Security */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">5. Data Security</h2>
            <p className="leading-relaxed">
              We implement appropriate technical and organizational measures to protect your information, including:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>Encryption of data in transit (HTTPS)</li>
              <li>Encrypted storage of passwords</li>
              <li>Regular security assessments</li>
              <li>Access controls and authentication measures</li>
            </ul>
            <p className="leading-relaxed mt-3">
              However, no method of transmission over the internet is 100% secure. While we strive to protect your information, we cannot guarantee absolute security.
            </p>
          </section>

          {/* Your Rights */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">6. Your Rights and Choices</h2>
            <p className="leading-relaxed">
              You have the following rights regarding your personal information:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li><strong>Access:</strong> You can access and review your information in your account settings</li>
              <li><strong>Correction:</strong> You can update your profile information at any time</li>
              <li><strong>Deletion:</strong> You can delete your account, which will remove your personal data (some data may be retained for legal purposes)</li>
              <li><strong>Data Portability:</strong> You can export your content and data</li>
              <li><strong>Opt-Out:</strong> You can opt out of non-essential communications</li>
            </ul>
            <p className="leading-relaxed mt-3">
              To exercise these rights, contact us through your account settings or our support channels.
            </p>
          </section>

          {/* Data Retention */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">7. Data Retention</h2>
            <p className="leading-relaxed">
              We retain your information for as long as your account is active or as needed to provide the Service. If you delete your account, we will delete or anonymize your information within a reasonable timeframe, except where we are required to retain it for legal, regulatory, or security purposes.
            </p>
          </section>

          {/* Children's Privacy */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">8. Children's Privacy</h2>
            <p className="leading-relaxed">
              Our Service is not intended for children under 13 years of age. We do not knowingly collect personal information from children under 13. If we become aware that we have collected information from a child under 13, we will take steps to delete it promptly.
            </p>
          </section>

          {/* International Users */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">9. International Data Transfers</h2>
            <p className="leading-relaxed">
              Your information may be transferred to and maintained on servers located outside your country, where data protection laws may differ. By using our Service, you consent to this transfer.
            </p>
          </section>

          {/* Changes to Policy */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">10. Changes to This Policy</h2>
            <p className="leading-relaxed">
              We may update this Privacy Policy from time to time. We will notify you of any material changes by posting the new policy on this page and updating the "Last updated" date. We encourage you to review this policy periodically.
            </p>
          </section>

          {/* Contact */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">11. Contact Us</h2>
            <p className="leading-relaxed">
              If you have questions or concerns about this Privacy Policy or our data practices, please contact us through our support channels or visit our{" "}
              <Link to="/" className="text-brand-soft hover:text-brand underline">
                website
              </Link>
              .
            </p>
          </section>
        </div>

        {/* Footer */}
        <div className="mt-12 pt-8 border-t border-border">
          <div className="flex flex-col sm:flex-row gap-4 justify-between items-center text-sm text-text-tertiary">
            <p>© 2024 Codewrinkles. All rights reserved.</p>
            <div className="flex gap-4">
              <Link to="/terms" className="hover:text-text-primary">
                Terms of Service
              </Link>
              <Link to="/" className="hover:text-text-primary">
                Home
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
