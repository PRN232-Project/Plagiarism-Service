using System;
using System.Collections.Generic;

namespace PRN232.Plagiarism.Domain.Entities;

public class PlagiarismRecord
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid ExamId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public bool HasViolations { get; set; }
    public DateTime ScannedAt { get; set; }

    // Navigation property for EF Core
    public List<PlagiarismViolationRecord> Violations { get; set; } = new();

    public List<string> ProjectGuids { get; set; } = new();
    public string WorkspacePath { get; set; } = string.Empty;

    public static PlagiarismRecord Create(Guid submissionId, Guid examId, string studentId, List<PlagiarismViolationRecord> violations, List<string> projectGuids, string workspacePath)
    {
        return new PlagiarismRecord
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            ExamId = examId,
            StudentId = studentId,
            HasViolations = violations.Count > 0,
            ScannedAt = DateTime.UtcNow,
            Violations = violations,
            ProjectGuids = projectGuids,
            WorkspacePath = workspacePath
        };
    }
}
