using System.Collections.Generic;
using System.Threading.Tasks;
using PRN232.Plagiarism.Application.DTOs;

namespace PRN232.Plagiarism.Application.Interfaces;

public interface IPlagiarismScanner
{
    Task<List<PlagiarismViolation>> ScanAsync(string workspacePath, List<string> bannedKeywords);
}
