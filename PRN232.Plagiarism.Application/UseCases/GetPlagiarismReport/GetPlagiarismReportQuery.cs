using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PRN232.Plagiarism.Application.DTOs;
using PRN232.Plagiarism.Application.Interfaces;

namespace PRN232.Plagiarism.Application.UseCases.GetPlagiarismReport;

public record GetPlagiarismReportQuery(Guid SubmissionId) : IRequest<PlagiarismReportDto?>;

public record PlagiarismReportDto(
    Guid Id,
    Guid SubmissionId,
    Guid ExamId,
    string StudentId,
    bool HasViolations,
    DateTime ScannedAt,
    List<PlagiarismViolation> Violations
);

public class GetPlagiarismReportQueryHandler : IRequestHandler<GetPlagiarismReportQuery, PlagiarismReportDto?>
{
    private readonly IPlagiarismRepository _repository;

    public GetPlagiarismReportQueryHandler(IPlagiarismRepository repository)
    {
        _repository = repository;
    }

    public async Task<PlagiarismReportDto?> Handle(GetPlagiarismReportQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetBySubmissionIdAsync(request.SubmissionId);
        if (record == null) return null;

        var violationsDto = record.Violations.Select(v => new PlagiarismViolation(
            v.FileName,
            v.BannedKeyword,
            v.LineNumber,
            v.CodeSnippet
        )).ToList();

        return new PlagiarismReportDto(
            record.Id,
            record.SubmissionId,
            record.ExamId,
            record.StudentId,
            record.HasViolations,
            record.ScannedAt,
            violationsDto
        );
    }
}
