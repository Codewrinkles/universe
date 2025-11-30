import { Link } from "react-router-dom";

export function TermsPage(): JSX.Element {
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
            Terms of Service
          </h1>
          <p className="text-sm text-text-secondary">
            Last updated: November 30, 2024
          </p>
        </div>

        {/* Content */}
        <div className="prose prose-invert max-w-none space-y-8 text-text-secondary">
          {/* Introduction */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">1. Introduction</h2>
            <p className="leading-relaxed">
              Welcome to Codewrinkles ("we," "our," or "us"). By accessing or using our platform at codewrinkles.com (the "Service"), you agree to be bound by these Terms of Service ("Terms"). If you disagree with any part of these terms, you may not access the Service.
            </p>
            <p className="leading-relaxed mt-3">
              Codewrinkles is an ecosystem of interconnected apps designed to create genuine connections and meaningful content. We reject algorithmic manipulation in favor of authentic engagement and value creation.
            </p>
          </section>

          {/* Account Registration */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">2. Account Registration</h2>
            <p className="leading-relaxed">
              To use certain features of the Service, you must register for an account. You agree to:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>Provide accurate, current, and complete information during registration</li>
              <li>Maintain and promptly update your account information</li>
              <li>Keep your password secure and confidential</li>
              <li>Immediately notify us of any unauthorized use of your account</li>
              <li>Be responsible for all activities that occur under your account</li>
            </ul>
            <p className="leading-relaxed mt-3">
              You must be at least 13 years old to use this Service. By registering, you represent that you meet this age requirement.
            </p>
          </section>

          {/* User Content */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">3. User Content</h2>
            <p className="leading-relaxed">
              You retain all rights to the content you create and share on Codewrinkles ("User Content"). By posting User Content, you grant us a worldwide, non-exclusive, royalty-free license to use, display, reproduce, and distribute your content in connection with operating and promoting the Service.
            </p>
            <p className="leading-relaxed mt-3">
              You are solely responsible for your User Content. You represent and warrant that:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>You own or have the necessary rights to post your User Content</li>
              <li>Your User Content does not violate any third-party rights</li>
              <li>Your User Content complies with these Terms and applicable laws</li>
            </ul>
          </section>

          {/* Prohibited Conduct */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">4. Prohibited Conduct</h2>
            <p className="leading-relaxed">
              You agree not to:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>Post content that is illegal, harmful, threatening, abusive, harassing, defamatory, or invasive of privacy</li>
              <li>Impersonate any person or entity, or falsely represent your affiliation with any person or entity</li>
              <li>Engage in spam, phishing, or other forms of unwanted solicitation</li>
              <li>Attempt to gain unauthorized access to the Service or other users' accounts</li>
              <li>Use automated systems (bots, scrapers) to access the Service without permission</li>
              <li>Interfere with or disrupt the Service or servers</li>
              <li>Circumvent any security or authentication measures</li>
              <li>Post content containing viruses, malware, or other harmful code</li>
            </ul>
          </section>

          {/* Intellectual Property */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">5. Intellectual Property</h2>
            <p className="leading-relaxed">
              The Service, including its design, code, graphics, and content (excluding User Content), is owned by Codewrinkles and protected by copyright, trademark, and other intellectual property laws.
            </p>
            <p className="leading-relaxed mt-3">
              While we build in public and share our codebase for learning purposes, the code and associated materials remain proprietary. You may not use, copy, modify, or distribute any part of the Service without our explicit permission.
            </p>
          </section>

          {/* Termination */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">6. Termination</h2>
            <p className="leading-relaxed">
              We reserve the right to suspend or terminate your account and access to the Service at our sole discretion, without notice, for conduct that we believe:
            </p>
            <ul className="list-disc list-inside space-y-2 mt-3 ml-4">
              <li>Violates these Terms</li>
              <li>Harms other users or the Service</li>
              <li>Exposes us or others to legal liability</li>
              <li>Is otherwise inappropriate or harmful</li>
            </ul>
            <p className="leading-relaxed mt-3">
              You may delete your account at any time through your account settings. Upon termination, your right to use the Service will immediately cease.
            </p>
          </section>

          {/* Disclaimers */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">7. Disclaimers</h2>
            <p className="leading-relaxed">
              THE SERVICE IS PROVIDED "AS IS" AND "AS AVAILABLE" WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE, OR NON-INFRINGEMENT.
            </p>
            <p className="leading-relaxed mt-3">
              We do not guarantee that the Service will be uninterrupted, secure, or error-free. We do not warrant the accuracy, completeness, or usefulness of any information on the Service.
            </p>
          </section>

          {/* Limitation of Liability */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">8. Limitation of Liability</h2>
            <p className="leading-relaxed">
              TO THE MAXIMUM EXTENT PERMITTED BY LAW, CODEWRINKLES SHALL NOT BE LIABLE FOR ANY INDIRECT, INCIDENTAL, SPECIAL, CONSEQUENTIAL, OR PUNITIVE DAMAGES, OR ANY LOSS OF PROFITS OR REVENUES, WHETHER INCURRED DIRECTLY OR INDIRECTLY, OR ANY LOSS OF DATA, USE, GOODWILL, OR OTHER INTANGIBLE LOSSES.
            </p>
          </section>

          {/* Changes to Terms */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">9. Changes to Terms</h2>
            <p className="leading-relaxed">
              We reserve the right to modify these Terms at any time. We will notify users of material changes via email or a notice on the Service. Your continued use of the Service after such modifications constitutes acceptance of the updated Terms.
            </p>
          </section>

          {/* Governing Law */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">10. Governing Law</h2>
            <p className="leading-relaxed">
              These Terms shall be governed by and construed in accordance with the laws of the jurisdiction in which Codewrinkles operates, without regard to its conflict of law provisions.
            </p>
          </section>

          {/* Contact */}
          <section>
            <h2 className="text-xl font-semibold text-text-primary mb-3">11. Contact Us</h2>
            <p className="leading-relaxed">
              If you have questions about these Terms, please contact us through our support channels or visit our{" "}
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
              <Link to="/privacy" className="hover:text-text-primary">
                Privacy Policy
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
