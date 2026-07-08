using System.Threading.Tasks;
using PRN232.Plagiarism.Domain.Entities;

namespace PRN232.Plagiarism.Application.Interfaces;

public interface IPlagiarismRepository
{
    Task SaveAsync(PlagiarismRecord record);
    Task<PlagiarismRecord?> GetBySubmissionIdAsync(System.Guid submissionId);
    Task<System.Collections.Generic.List<PlagiarismRecord>> GetByExamIdAsync(System.Guid examId);
    Task SaveComparisonAsync(PlagiarismComparison comparison);
    Task DeleteComparisonsBySubmissionAsync(System.Guid submissionId);
    Task<System.Collections.Generic.List<PlagiarismComparison>> GetComparisonsByExamIdAsync(System.Guid examId);
}
