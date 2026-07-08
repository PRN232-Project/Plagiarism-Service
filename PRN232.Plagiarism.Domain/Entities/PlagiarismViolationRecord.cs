using System;

namespace PRN232.Plagiarism.Domain.Entities;

public class PlagiarismViolationRecord
{
    public Guid Id { get; set; }
    public Guid PlagiarismRecordId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string BannedKeyword { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string CodeSnippet { get; set; } = string.Empty;
    
    // EF Navigation property
    public PlagiarismRecord? PlagiarismRecord { get; set; }
}
