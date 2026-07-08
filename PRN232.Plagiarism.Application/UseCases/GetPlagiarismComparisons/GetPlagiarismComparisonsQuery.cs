using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PRN232.Plagiarism.Application.Interfaces;
using PRN232.Plagiarism.Domain.Entities;

namespace PRN232.Plagiarism.Application.UseCases.GetPlagiarismComparisons;

public record GetPlagiarismComparisonsQuery(Guid ExamId) : IRequest<List<PlagiarismComparison>>;

public class GetPlagiarismComparisonsQueryHandler : IRequestHandler<GetPlagiarismComparisonsQuery, List<PlagiarismComparison>>
{
    private readonly IPlagiarismRepository _repository;

    public GetPlagiarismComparisonsQueryHandler(IPlagiarismRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PlagiarismComparison>> Handle(GetPlagiarismComparisonsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetComparisonsByExamIdAsync(request.ExamId);
    }
}
