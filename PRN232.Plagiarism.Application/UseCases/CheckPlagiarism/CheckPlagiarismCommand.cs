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
        // 1. Quét từ khóa cấm
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

        // 2. Quét Project GUIDs
        var guidScanner = new Services.VsProjectGuidScanner();
        var projectGuids = guidScanner.ScanProjectGuids(request.WorkspacePath);

        // 3. Tạo và lưu PlagiarismRecord cho sinh viên hiện tại
        var record = PlagiarismRecord.Create(
            request.SubmissionId,
            request.ExamId,
            request.StudentId,
            violationRecords,
            projectGuids,
            request.WorkspacePath
        );

        // Lưu vào database riêng của plagiarism thông qua repository
        await _repository.SaveAsync(record);

        // 4. Xóa các báo cáo so sánh chéo cũ liên quan đến bài nộp này
        await _repository.DeleteComparisonsBySubmissionAsync(request.SubmissionId);

        // 5. Thực hiện so sánh chéo với các sinh viên khác của cùng một Kỳ thi (ExamId)
        var otherRecords = await _repository.GetByExamIdAsync(request.ExamId);
        
        var winnowingChecker = new Services.WinnowingSimilarityChecker();
        var currentFingerprints = winnowingChecker.GetFingerprints(request.WorkspacePath);

        foreach (var other in otherRecords)
        {
            if (other.SubmissionId == request.SubmissionId)
            {
                continue;
            }

            // A. Kiểm tra trùng GUID chéo
            bool guidMatched = record.ProjectGuids.Intersect(other.ProjectGuids).Any();

            // B. Đo độ tương đồng thuật toán Winnowing
            var otherFingerprints = winnowingChecker.GetFingerprints(other.WorkspacePath);
            var similarity = winnowingChecker.CalculateSimilarity(currentFingerprints, otherFingerprints);

            // C. Lưu báo cáo so sánh chéo nếu có nghi vấn (Trùng GUID hoặc độ tương đồng >= 50%)
            if (guidMatched || similarity >= 0.50m)
            {
                var comparison = PlagiarismComparison.Create(
                    request.ExamId,
                    request.SubmissionId,
                    other.SubmissionId,
                    request.StudentId,
                    other.StudentId,
                    similarity * 100, // Đổi sang phần trăm (ví dụ: 85.50%)
                    guidMatched
                );
                await _repository.SaveComparisonAsync(comparison);
            }
        }

        // 6. Gửi thông báo gian lận từ khóa cấm nếu có vi phạm
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
