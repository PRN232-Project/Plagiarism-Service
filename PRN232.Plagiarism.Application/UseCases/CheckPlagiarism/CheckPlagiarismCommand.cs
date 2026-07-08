using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PRN232.Plagiarism.Application.Events;
using PRN232.Plagiarism.Application.Interfaces;
using PRN232.Plagiarism.Domain.Entities;

namespace PRN232.Plagiarism.Application.UseCases.CheckPlagiarism;

public record CheckPlagiarismCommand(
    Guid SubmissionId,
    Guid ExamId,
    string StudentId,
    string WorkspacePath,
    List<string> BannedKeywords
) : IRequest;

public class CheckPlagiarismCommandHandler : IRequestHandler<CheckPlagiarismCommand>
{
    private readonly IPlagiarismScanner _scanner;
    private readonly IPlagiarismAlertPublisher _publisher;
    private readonly IPlagiarismRepository _repository;

    public CheckPlagiarismCommandHandler(
        IPlagiarismScanner scanner,
        IPlagiarismAlertPublisher publisher,
        IPlagiarismRepository repository)
    {
        _scanner = scanner;
        _publisher = publisher;
        _repository = repository;
    }

    public async Task Handle(CheckPlagiarismCommand request, CancellationToken cancellationToken)
    {
        var violations = await _scanner.ScanAsync(request.WorkspacePath, request.BannedKeywords);

        // Map DTOs to Domain Entities
        var violationRecords = violations.Select(v => new PlagiarismViolationRecord
        {
            Id = Guid.NewGuid(),
            FileName = v.FileName,
            BannedKeyword = v.BannedKeyword,
            LineNumber = v.LineNumber,
            CodeSnippet = v.CodeSnippet
        }).ToList();

        var record = PlagiarismRecord.Create(
            request.SubmissionId,
            request.ExamId,
            request.StudentId,
            violationRecords
        );

        // Lưu vào database riêng của plagiarism thông qua repository
        await _repository.SaveAsync(record);

        // Gửi thông báo gian lận nếu có vi phạm
        if (violations.Count > 0)
        {
            var alertEvent = new PlagiarismAlertEvent(
                request.SubmissionId,
                request.ExamId,
                request.StudentId,
                violations
            );
            await _publisher.PublishAlertAsync(alertEvent);
        }
    }
}
