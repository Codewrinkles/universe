/**
 * Admin page for managing Nova content ingestion
 * Allows admins to upload PDFs, transcripts, and scrape documentation
 */

import { useEffect, useState, useRef, useCallback } from "react";
import { Card } from "../../components/ui/Card";
import { config } from "../../config";

interface IngestionJob {
  id: string;
  source: "book" | "youtube" | "officialdocs" | "article" | "pulse";
  status: "queued" | "processing" | "completed" | "failed";
  title: string;
  author: string | null;
  technology: string | null;
  sourceUrl: string | null;
  chunksCreated: number;
  totalPages: number | null;
  pagesProcessed: number | null;
  errorMessage: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

type StatusFilter = "all" | "queued" | "processing" | "completed" | "failed";
type UploadTab = "pdf" | "transcript" | "docs";

export function ContentIngestionPage(): JSX.Element {
  const [jobs, setJobs] = useState<IngestionJob[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [activeTab, setActiveTab] = useState<UploadTab>("pdf");
  const [isUploading, setIsUploading] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState<string | null>(null);

  // PDF form state
  const [pdfTitle, setPdfTitle] = useState("");
  const [pdfContentType, setPdfContentType] = useState<"book" | "officialdocs">("book");
  const [pdfAuthor, setPdfAuthor] = useState("");
  const [pdfTechnology, setPdfTechnology] = useState("");
  const [pdfFile, setPdfFile] = useState<File | null>(null);
  const pdfInputRef = useRef<HTMLInputElement>(null);

  // Transcript form state
  const [transcriptTitle, setTranscriptTitle] = useState("");
  const [transcriptUrl, setTranscriptUrl] = useState("");
  const [transcriptText, setTranscriptText] = useState("");

  // Docs form state
  const [docsUrl, setDocsUrl] = useState("");
  const [docsTechnology, setDocsTechnology] = useState("");
  const [docsMaxPages, setDocsMaxPages] = useState("100");

  const fetchJobs = useCallback(async (): Promise<void> => {
    try {
      setError(null);
      const token = localStorage.getItem(config.auth.accessTokenKey);
      const url = statusFilter === "all"
        ? `${config.api.baseUrl}/api/admin/nova/content/jobs`
        : `${config.api.baseUrl}/api/admin/nova/content/jobs?status=${statusFilter}`;

      const response = await fetch(url, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error("Failed to fetch jobs");
      }

      const data = await response.json();
      setJobs(data.jobs);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load jobs");
    } finally {
      setIsLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => {
    void fetchJobs();
  }, [fetchJobs]);

  // Auto-refresh every 5 seconds if there are queued or processing jobs
  useEffect(() => {
    const hasActiveJobs = jobs.some(j => j.status === "queued" || j.status === "processing");
    if (!hasActiveJobs) return;

    const interval = setInterval(() => {
      void fetchJobs();
    }, 5000);

    return () => clearInterval(interval);
  }, [jobs, fetchJobs]);

  const handlePdfUpload = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();

    // Validate based on content type
    if (!pdfFile || !pdfTitle.trim()) return;
    if (pdfContentType === "book" && !pdfAuthor.trim()) return;
    if (pdfContentType === "officialdocs" && !pdfTechnology.trim()) return;

    try {
      setIsUploading(true);
      setError(null);

      const token = localStorage.getItem(config.auth.accessTokenKey);
      const formData = new FormData();
      formData.append("file", pdfFile);
      formData.append("title", pdfTitle.trim());
      formData.append("contentType", pdfContentType);

      if (pdfContentType === "book") {
        formData.append("author", pdfAuthor.trim());
      } else {
        formData.append("technology", pdfTechnology.trim());
      }

      const response = await fetch(
        `${config.api.baseUrl}/api/admin/nova/content/pdf`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
          },
          body: formData,
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "Failed to upload PDF");
      }

      // Reset form
      setPdfTitle("");
      setPdfContentType("book");
      setPdfAuthor("");
      setPdfTechnology("");
      setPdfFile(null);
      if (pdfInputRef.current) {
        pdfInputRef.current.value = "";
      }

      // Refresh jobs list
      await fetchJobs();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to upload PDF");
    } finally {
      setIsUploading(false);
    }
  };

  const handleTranscriptUpload = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    if (!transcriptText.trim() || !transcriptTitle.trim() || !transcriptUrl.trim()) return;

    try {
      setIsUploading(true);
      setError(null);

      const token = localStorage.getItem(config.auth.accessTokenKey);
      const response = await fetch(
        `${config.api.baseUrl}/api/admin/nova/content/transcript`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            videoUrl: transcriptUrl.trim(),
            title: transcriptTitle.trim(),
            transcript: transcriptText,
          }),
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "Failed to upload transcript");
      }

      // Reset form
      setTranscriptTitle("");
      setTranscriptUrl("");
      setTranscriptText("");

      // Refresh jobs list
      await fetchJobs();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to upload transcript");
    } finally {
      setIsUploading(false);
    }
  };

  const handleDocsScrape = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault();
    if (!docsUrl.trim() || !docsTechnology.trim()) return;

    try {
      setIsUploading(true);
      setError(null);

      const token = localStorage.getItem(config.auth.accessTokenKey);
      const response = await fetch(
        `${config.api.baseUrl}/api/admin/nova/content/docs`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            homepageUrl: docsUrl.trim(),
            technology: docsTechnology.trim(),
            maxPages: parseInt(docsMaxPages, 10) || 100,
          }),
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "Failed to start docs scrape");
      }

      // Reset form
      setDocsUrl("");
      setDocsTechnology("");
      setDocsMaxPages("100");

      // Refresh jobs list
      await fetchJobs();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to start docs scrape");
    } finally {
      setIsUploading(false);
    }
  };

  const handleDelete = async (id: string): Promise<void> => {
    try {
      setDeleteLoading(id);
      const token = localStorage.getItem(config.auth.accessTokenKey);

      const response = await fetch(
        `${config.api.baseUrl}/api/admin/nova/content/jobs/${id}`,
        {
          method: "DELETE",
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        const errorData = await response.json().catch(() => null);
        throw new Error(errorData?.message || "Failed to delete job");
      }

      await fetchJobs();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete job");
    } finally {
      setDeleteLoading(null);
    }
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString("en-US", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const getSourceIcon = (source: string): string => {
    switch (source) {
      case "book": return "📚";
      case "youtube": return "📺";
      case "officialdocs": return "📄";
      case "article": return "📰";
      case "pulse": return "💬";
      default: return "📄";
    }
  };

  const getStatusBadge = (status: string): JSX.Element => {
    const styles = {
      queued: "bg-yellow-500/20 text-yellow-300 border-yellow-500/30",
      processing: "bg-blue-500/20 text-blue-300 border-blue-500/30",
      completed: "bg-emerald-500/20 text-emerald-300 border-emerald-500/30",
      failed: "bg-red-500/20 text-red-300 border-red-500/30",
    };

    return (
      <span
        className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${styles[status as keyof typeof styles] || styles.queued}`}
      >
        {status}
      </span>
    );
  };

  const getProgress = (job: IngestionJob): string | null => {
    if (job.status !== "processing") return null;
    if (job.totalPages && job.pagesProcessed !== null) {
      return `${job.pagesProcessed}/${job.totalPages} pages`;
    }
    return "Processing...";
  };

  const queuedCount = jobs.filter(j => j.status === "queued").length;
  const processingCount = jobs.filter(j => j.status === "processing").length;
  const completedCount = jobs.filter(j => j.status === "completed").length;
  const failedCount = jobs.filter(j => j.status === "failed").length;

  return (
    <div className="p-6">
      <div className="mb-6">
        <h1 className="text-xl font-bold text-text-primary">Content Ingestion</h1>
        <p className="text-sm text-text-secondary mt-1">
          Manage Nova RAG knowledge base content
        </p>
      </div>

      {/* Upload Forms */}
      <Card className="mb-6">
        <div className="flex gap-2 mb-4 border-b border-border pb-4">
          {(["pdf", "transcript", "docs"] as UploadTab[]).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-4 py-2 text-sm font-medium rounded-full transition-colors ${
                activeTab === tab
                  ? "bg-violet-600 text-white"
                  : "bg-surface-card1 text-text-secondary hover:text-text-primary"
              }`}
            >
              {tab === "pdf" && "📚 Upload PDF"}
              {tab === "transcript" && "📺 YouTube Transcript"}
              {tab === "docs" && "📄 Scrape Docs"}
            </button>
          ))}
        </div>

        {/* PDF Upload Form */}
        {activeTab === "pdf" && (
          <form onSubmit={(e) => void handlePdfUpload(e)} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                Content Type
              </label>
              <div className="flex gap-2">
                <button
                  type="button"
                  onClick={() => setPdfContentType("book")}
                  className={`px-4 py-2 text-sm font-medium rounded-full transition-colors ${
                    pdfContentType === "book"
                      ? "bg-violet-600 text-white"
                      : "bg-surface-card1 text-text-secondary hover:text-text-primary border border-border"
                  }`}
                >
                  📚 Book
                </button>
                <button
                  type="button"
                  onClick={() => setPdfContentType("officialdocs")}
                  className={`px-4 py-2 text-sm font-medium rounded-full transition-colors ${
                    pdfContentType === "officialdocs"
                      ? "bg-violet-600 text-white"
                      : "bg-surface-card1 text-text-secondary hover:text-text-primary border border-border"
                  }`}
                >
                  📄 Official Docs
                </button>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                {pdfContentType === "book" ? "Book Title" : "Documentation Title"}
              </label>
              <input
                type="text"
                value={pdfTitle}
                onChange={(e) => setPdfTitle(e.target.value)}
                placeholder={pdfContentType === "book" ? "e.g., Domain-Driven Design" : "e.g., ASP.NET Core Documentation"}
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
              />
            </div>
            {pdfContentType === "book" && (
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-1">
                  Author
                </label>
                <input
                  type="text"
                  value={pdfAuthor}
                  onChange={(e) => setPdfAuthor(e.target.value)}
                  placeholder="e.g., Eric Evans"
                  className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
                />
              </div>
            )}
            {pdfContentType === "officialdocs" && (
              <div>
                <label className="block text-sm font-medium text-text-secondary mb-1">
                  Technology
                </label>
                <input
                  type="text"
                  value={pdfTechnology}
                  onChange={(e) => setPdfTechnology(e.target.value)}
                  placeholder="e.g., aspnetcore, efcore, dotnet"
                  className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
                />
              </div>
            )}
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                PDF File
              </label>
              <input
                ref={pdfInputRef}
                type="file"
                accept=".pdf"
                onChange={(e) => setPdfFile(e.target.files?.[0] || null)}
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-medium file:bg-violet-600 file:text-white hover:file:bg-violet-500"
              />
            </div>
            <button
              type="submit"
              disabled={
                isUploading ||
                !pdfFile ||
                !pdfTitle.trim() ||
                (pdfContentType === "book" && !pdfAuthor.trim()) ||
                (pdfContentType === "officialdocs" && !pdfTechnology.trim())
              }
              className="px-4 py-2 text-sm font-medium rounded-full bg-violet-600 text-white hover:bg-violet-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isUploading ? "Uploading..." : "Upload PDF"}
            </button>
          </form>
        )}

        {/* Transcript Upload Form */}
        {activeTab === "transcript" && (
          <form onSubmit={(e) => void handleTranscriptUpload(e)} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                Video Title
              </label>
              <input
                type="text"
                value={transcriptTitle}
                onChange={(e) => setTranscriptTitle(e.target.value)}
                placeholder="e.g., Clean Architecture in .NET"
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                YouTube URL
              </label>
              <input
                type="url"
                value={transcriptUrl}
                onChange={(e) => setTranscriptUrl(e.target.value)}
                placeholder="https://www.youtube.com/watch?v=..."
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                Transcript
              </label>
              <textarea
                value={transcriptText}
                onChange={(e) => setTranscriptText(e.target.value)}
                placeholder="Paste the transcript here..."
                rows={6}
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500 resize-none"
              />
            </div>
            <button
              type="submit"
              disabled={isUploading || !transcriptText.trim() || !transcriptTitle.trim() || !transcriptUrl.trim()}
              className="px-4 py-2 text-sm font-medium rounded-full bg-violet-600 text-white hover:bg-violet-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isUploading ? "Uploading..." : "Upload Transcript"}
            </button>
          </form>
        )}

        {/* Docs Scrape Form */}
        {activeTab === "docs" && (
          <form onSubmit={(e) => void handleDocsScrape(e)} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                Documentation Homepage URL
              </label>
              <input
                type="url"
                value={docsUrl}
                onChange={(e) => setDocsUrl(e.target.value)}
                placeholder="https://learn.microsoft.com/en-us/dotnet/fundamentals/"
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                Technology
              </label>
              <input
                type="text"
                value={docsTechnology}
                onChange={(e) => setDocsTechnology(e.target.value)}
                placeholder="e.g., dotnet, react, typescript"
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-text-secondary mb-1">
                Max Pages (1-500)
              </label>
              <input
                type="number"
                value={docsMaxPages}
                onChange={(e) => setDocsMaxPages(e.target.value)}
                min="1"
                max="500"
                className="w-full px-3 py-2 bg-surface-card1 border border-border rounded-lg text-text-primary placeholder:text-text-tertiary focus:outline-none focus:border-violet-500"
              />
            </div>
            <button
              type="submit"
              disabled={isUploading || !docsUrl.trim() || !docsTechnology.trim()}
              className="px-4 py-2 text-sm font-medium rounded-full bg-violet-600 text-white hover:bg-violet-500 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isUploading ? "Starting..." : "Start Scraping"}
            </button>
          </form>
        )}
      </Card>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4 mb-6">
        <Card className="text-center">
          <p className="text-3xl font-bold text-yellow-400">{queuedCount}</p>
          <p className="text-xs text-text-tertiary">Queued</p>
        </Card>
        <Card className="text-center">
          <p className="text-3xl font-bold text-blue-400">{processingCount}</p>
          <p className="text-xs text-text-tertiary">Processing</p>
        </Card>
        <Card className="text-center">
          <p className="text-3xl font-bold text-emerald-400">{completedCount}</p>
          <p className="text-xs text-text-tertiary">Completed</p>
        </Card>
        <Card className="text-center">
          <p className="text-3xl font-bold text-red-400">{failedCount}</p>
          <p className="text-xs text-text-tertiary">Failed</p>
        </Card>
      </div>

      {/* Filter tabs */}
      <div className="flex gap-2 mb-6">
        {(["all", "queued", "processing", "completed", "failed"] as StatusFilter[]).map((filter) => (
          <button
            key={filter}
            onClick={() => setStatusFilter(filter)}
            className={`px-4 py-2 text-sm font-medium rounded-full transition-colors ${
              statusFilter === filter
                ? "bg-violet-600 text-white"
                : "bg-surface-card1 text-text-secondary hover:text-text-primary"
            }`}
          >
            {filter.charAt(0).toUpperCase() + filter.slice(1)}
          </button>
        ))}
      </div>

      {/* Error message */}
      {error && (
        <div className="mb-4 rounded-lg border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {error}
          <button
            onClick={() => setError(null)}
            className="ml-2 text-red-300 hover:text-red-200"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <p className="text-sm text-text-secondary">Loading jobs...</p>
      )}

      {/* Jobs list */}
      {!isLoading && jobs.length === 0 && (
        <Card>
          <p className="text-sm text-text-secondary text-center py-8">
            No {statusFilter === "all" ? "" : statusFilter} jobs found
          </p>
        </Card>
      )}

      {!isLoading && jobs.length > 0 && (
        <div className="space-y-4">
          {jobs.map((job) => (
            <Card key={job.id} className="relative">
              <div className="flex justify-between items-start mb-3">
                <div className="flex items-center gap-2">
                  <span className="text-lg">{getSourceIcon(job.source)}</span>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="text-sm font-semibold text-text-primary">{job.title}</h3>
                      {getStatusBadge(job.status)}
                    </div>
                    {job.author && (
                      <p className="text-xs text-text-tertiary">by {job.author}</p>
                    )}
                    {job.technology && (
                      <p className="text-xs text-text-tertiary">{job.technology}</p>
                    )}
                  </div>
                </div>
                <p className="text-xs text-text-tertiary">{formatDate(job.createdAt)}</p>
              </div>

              <div className="flex items-center gap-4 text-xs text-text-secondary">
                <span>{job.chunksCreated} chunks</span>
                {getProgress(job) && (
                  <span className="text-blue-400">{getProgress(job)}</span>
                )}
                {job.sourceUrl && (
                  <a
                    href={job.sourceUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-violet-400 hover:text-violet-300 truncate max-w-xs"
                  >
                    {job.sourceUrl}
                  </a>
                )}
              </div>

              {/* Error message */}
              {job.status === "failed" && job.errorMessage && (
                <div className="mt-3 p-2 rounded-lg bg-red-500/10 border border-red-500/30">
                  <p className="text-xs text-red-400">{job.errorMessage}</p>
                </div>
              )}

              {/* Delete button for completed/failed jobs */}
              {(job.status === "completed" || job.status === "failed") && (
                <div className="mt-4 pt-4 border-t border-border">
                  <button
                    onClick={() => void handleDelete(job.id)}
                    disabled={deleteLoading === job.id}
                    className="px-4 py-2 text-sm font-medium rounded-full bg-red-600/20 text-red-400 hover:bg-red-600/30 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {deleteLoading === job.id ? "Deleting..." : "Delete Job & Content"}
                  </button>
                </div>
              )}
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
