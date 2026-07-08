using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN232.Plagiarism.Application.Interfaces;
using PRN232.Plagiarism.Domain.Entities;

namespace PRN232.Plagiarism.Infrastructure.Persistence.Repositories;

public class PlagiarismRepository : IPlagiarismRepository
{
    private readonly PlagiarismDbContext _context;

    public PlagiarismRepository(PlagiarismDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(PlagiarismRecord record)
    {
        var existing = await _context.PlagiarismRecords
            .FirstOrDefaultAsync(r => r.SubmissionId == record.SubmissionId);
        if (existing != null)
        {
            _context.PlagiarismRecords.Remove(existing);
        }
        
        _context.PlagiarismRecords.Add(record);
        await _context.SaveChangesAsync();
    }

    public async Task<PlagiarismRecord?> GetBySubmissionIdAsync(System.Guid submissionId)
    {
        return await _context.PlagiarismRecords
            .Include(r => r.Violations)
            .FirstOrDefaultAsync(r => r.SubmissionId == submissionId);
    }

    public async Task<System.Collections.Generic.List<PlagiarismRecord>> GetByExamIdAsync(System.Guid examId)
    {
        return await _context.PlagiarismRecords
            .Include(r => r.Violations)
            .Where(r => r.ExamId == examId)
            .ToListAsync();
    }

    public async Task SaveComparisonAsync(PlagiarismComparison comparison)
    {
        _context.PlagiarismComparisons.Add(comparison);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteComparisonsBySubmissionAsync(System.Guid submissionId)
    {
        var comparisons = await _context.PlagiarismComparisons
            .Where(c => c.SubmissionIdA == submissionId || c.SubmissionIdB == submissionId)
            .ToListAsync();
        _context.PlagiarismComparisons.RemoveRange(comparisons);
        await _context.SaveChangesAsync();
    }

    public async Task<System.Collections.Generic.List<PlagiarismComparison>> GetComparisonsByExamIdAsync(System.Guid examId)
    {
        return await _context.PlagiarismComparisons
            .Where(c => c.ExamId == examId)
            .OrderByDescending(c => c.SimilarityScore)
            .ToListAsync();
    }
}
