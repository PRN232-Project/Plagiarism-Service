using System;
using System.Collections.Generic;
using PRN232.Plagiarism.Application.DTOs;

namespace PRN232.Plagiarism.Application.Events;

public record PlagiarismAlertEvent(
    Guid SubmissionId,
    Guid ExamId,
    string StudentId,
    List<PlagiarismViolation> Violations
);
