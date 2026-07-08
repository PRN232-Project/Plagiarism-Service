using System;

namespace PRN232.Plagiarism.Domain.Entities;

public class PlagiarismComparison
{
    public Guid Id { get; set; }
    public Guid ExamId { get; set; }
    public Guid SubmissionIdA { get; set; }
    public Guid SubmissionIdB { get; set; }
    public string StudentIdA { get; set; } = string.Empty;
    public string StudentIdB { get; set; } = string.Empty;
    public decimal SimilarityScore { get; set; }
    public bool GuidMatched { get; set; }
    public DateTime ScannedAt { get; set; }

    public static PlagiarismComparison Create(
        Guid examId,
        Guid submissionIdA,
        Guid submissionIdB,
        string studentIdA,
        string studentIdB,
        decimal similarityScore,
        bool guidMatched)
    {
        return new PlagiarismComparison
        {
            Id = Guid.NewGuid(),
            ExamId = examId,
            SubmissionIdA = submissionIdA,
            SubmissionIdB = submissionIdB,
            StudentIdA = studentIdA,
            StudentIdB = studentIdB,
            SimilarityScore = similarityScore,
            GuidMatched = guidMatched,
            ScannedAt = DateTime.UtcNow
        };
    }
}
