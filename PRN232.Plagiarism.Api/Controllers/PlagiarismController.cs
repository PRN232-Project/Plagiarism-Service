using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using PRN232.Plagiarism.Application.UseCases.CheckPlagiarism;
using PRN232.Plagiarism.Application.UseCases.GetPlagiarismReport;

namespace PRN232.Plagiarism.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlagiarismController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlagiarismController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("submissions/{submissionId}")]
    public async Task<ActionResult<PlagiarismReportDto>> GetBySubmissionId(Guid submissionId)
    {
        var result = await _mediator.Send(new GetPlagiarismReportQuery(submissionId));
        if (result == null)
        {
            return NotFound($"Không tìm thấy báo cáo quét trùng lặp cho Submission ID: {submissionId}");
        }
        return Ok(result);
    }

    [HttpPost("check")]
    public async Task<IActionResult> RunCheck([FromBody] RunCheckRequest request)
    {
        var command = new CheckPlagiarismCommand(
            request.SubmissionId ?? Guid.NewGuid(),
            request.ExamId,
            request.StudentId,
            request.WorkspacePath,
            request.BannedKeywords
        );

        await _mediator.Send(command);
        return Ok(new { Message = "Đã kích hoạt quét mã nguồn gian lận thành công." });
    }
}

public class RunCheckRequest
{
    public Guid? SubmissionId { get; set; }
    public Guid ExamId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string WorkspacePath { get; set; } = string.Empty;
    public System.Collections.Generic.List<string> BannedKeywords { get; set; } = new();
}
