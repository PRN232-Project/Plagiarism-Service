using System;
using System.Collections.Generic;

namespace PRN232.Plagiarism.Api.Requests;

public class RunCheckRequest
{
    public Guid? SubmissionId { get; set; }
    public Guid ExamId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string WorkspacePath { get; set; } = string.Empty;
    public List<string> BannedKeywords { get; set; } = new();
}
