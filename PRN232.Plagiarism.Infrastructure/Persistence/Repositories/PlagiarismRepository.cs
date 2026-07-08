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
}
