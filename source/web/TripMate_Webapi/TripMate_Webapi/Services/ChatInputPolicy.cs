namespace TripMate_WebAPI.Services;

public static class ChatInputPolicy
{
    public const int MaxMessageLength = 4_000;
    public const long MaxAttachmentBytes = 10 * 1024 * 1024;
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    private static readonly IReadOnlyDictionary<string, ISet<string>> AllowedAttachments =
        new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
            [".jpeg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
            [".png"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/png" },
            [".gif"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/gif" },
            [".webp"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/webp" },
            [".pdf"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/pdf" },
            [".txt"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "text/plain" },
            [".doc"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/msword" },
            [".docx"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            [".xls"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/vnd.ms-excel" },
            [".xlsx"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            [".mp3"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "audio/mpeg" },
            [".mp4"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "video/mp4" }
        };

    public static string NormalizeMessage(string? content)
    {
        var normalized = content?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException("Message content is required.", nameof(content));
        if (normalized.Length > MaxMessageLength)
            throw new ArgumentException($"Messages cannot exceed {MaxMessageLength} characters.", nameof(content));
        return normalized;
    }

    public static (int Limit, int Offset) NormalizePage(int? limit, int? offset)
        => (Math.Clamp(limit ?? DefaultPageSize, 1, MaxPageSize), Math.Max(offset ?? 0, 0));

    public static string? ValidateAttachment(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "No file provided.";
        if (file.Length > MaxAttachmentBytes) return "Attachments cannot exceed 10 MB.";

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedAttachments.TryGetValue(extension, out var allowedContentTypes) ||
            !allowedContentTypes.Contains(file.ContentType))
        {
            return "Unsupported attachment type.";
        }

        return null;
    }
}
