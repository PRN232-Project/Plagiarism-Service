namespace PRN232.Plagiarism.Application.DTOs;

public record PlagiarismViolation(
    string FileName,
    string BannedKeyword,
    int LineNumber,
    string CodeSnippet
);
