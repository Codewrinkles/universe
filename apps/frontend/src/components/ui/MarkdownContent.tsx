import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeRaw from "rehype-raw";
import { Prism as SyntaxHighlighter } from "react-syntax-highlighter";
import { oneDark } from "react-syntax-highlighter/dist/esm/styles/prism";
import type { Components } from "react-markdown";

interface MarkdownContentProps {
  content: string;
}

/**
 * MarkdownContent - Renders markdown with syntax highlighting
 *
 * Features:
 * - Code blocks with syntax highlighting (Prism + One Dark theme)
 * - Inline code styling
 * - Proper heading, list, and link styling
 * - Dark theme compatible
 */
export function MarkdownContent({ content }: MarkdownContentProps): JSX.Element {
  const components: Components = {
    // Code blocks and inline code
    code({ className, children, ...props }) {
      const match = /language-(\w+)/.exec(className || "");
      const codeString = String(children).replace(/\n$/, "");

      // Check if this is a code block (has language) or inline code
      const isCodeBlock = match || (codeString.includes("\n"));

      if (isCodeBlock) {
        return (
          <div className="my-3 rounded-lg overflow-hidden">
            <SyntaxHighlighter
              style={oneDark}
              language={match?.[1] || "text"}
              PreTag="div"
              customStyle={{
                margin: 0,
                padding: "1rem",
                fontSize: "0.8125rem",
                lineHeight: "1.5",
                borderRadius: "0.5rem",
              }}
            >
              {codeString}
            </SyntaxHighlighter>
          </div>
        );
      }

      // Inline code
      return (
        <code
          className="px-1.5 py-0.5 rounded bg-surface-card2 text-violet-300 text-[0.8125rem] font-mono"
          {...props}
        >
          {children}
        </code>
      );
    },

    // Paragraphs
    p({ children }) {
      return <p className="mb-3 last:mb-0">{children}</p>;
    },

    // Headings
    h1({ children }) {
      return <h1 className="text-lg font-semibold text-text-primary mb-3 mt-4 first:mt-0">{children}</h1>;
    },
    h2({ children }) {
      return <h2 className="text-base font-semibold text-text-primary mb-2 mt-4 first:mt-0">{children}</h2>;
    },
    h3({ children }) {
      return <h3 className="text-sm font-semibold text-text-primary mb-2 mt-3 first:mt-0">{children}</h3>;
    },

    // Lists
    ul({ children }) {
      return <ul className="list-disc list-outside ml-5 mb-3 space-y-1">{children}</ul>;
    },
    ol({ children }) {
      return <ol className="list-decimal list-outside ml-5 mb-3 space-y-1">{children}</ol>;
    },
    li({ children }) {
      return <li className="text-text-primary">{children}</li>;
    },

    // Links
    a({ href, children }) {
      return (
        <a
          href={href}
          target="_blank"
          rel="noopener noreferrer"
          className="text-violet-400 hover:text-violet-300 underline underline-offset-2"
        >
          {children}
        </a>
      );
    },

    // Bold and italic
    strong({ children }) {
      return <strong className="font-semibold text-text-primary">{children}</strong>;
    },
    em({ children }) {
      return <em className="italic">{children}</em>;
    },

    // Blockquotes
    blockquote({ children }) {
      return (
        <blockquote className="border-l-2 border-violet-500/50 pl-4 my-3 text-text-secondary italic">
          {children}
        </blockquote>
      );
    },

    // Horizontal rule
    hr() {
      return <hr className="border-border my-4" />;
    },

    // Tables
    table({ children }) {
      return (
        <div className="my-4 overflow-x-auto">
          <table className="w-full border-collapse text-sm">
            {children}
          </table>
        </div>
      );
    },
    thead({ children }) {
      return <thead className="bg-surface-card2">{children}</thead>;
    },
    tbody({ children }) {
      return <tbody className="divide-y divide-border">{children}</tbody>;
    },
    tr({ children }) {
      return <tr className="border-b border-border">{children}</tr>;
    },
    th({ children }) {
      return (
        <th className="px-3 py-2 text-left text-xs font-semibold text-text-primary border border-border">
          {children}
        </th>
      );
    },
    td({ children }) {
      return (
        <td className="px-3 py-2 text-text-secondary border border-border">
          {children}
        </td>
      );
    },
  };

  return (
    <div className="text-sm text-text-primary leading-relaxed prose-invert max-w-none">
      <ReactMarkdown
        remarkPlugins={[remarkGfm]}
        rehypePlugins={[rehypeRaw]}
        components={components}
      >
        {content}
      </ReactMarkdown>
    </div>
  );
}
