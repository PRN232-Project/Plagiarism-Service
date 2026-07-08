using System;
using System.Collections.Generic;

namespace PRN232.Plagiarism.Application.Events;

public record SubmissionGradedEvent(
    Guid SubmissionId,
    Guid ExamId,
    string StudentId,
    string WorkspacePath,
    List<string> BannedKeywords
);
