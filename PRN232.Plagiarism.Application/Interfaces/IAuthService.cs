using System.Threading.Tasks;
using PRN232.Plagiarism.Application.DTOs;

namespace PRN232.Plagiarism.Application.Interfaces;

public interface IAuthService
{
    Task<UserClaimsDto> VerifyTokenAsync(string token);
}
