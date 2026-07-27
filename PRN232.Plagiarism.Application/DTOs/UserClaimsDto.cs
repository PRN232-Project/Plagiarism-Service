namespace PRN232.Plagiarism.Application.DTOs;

public class UserClaimsDto
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
