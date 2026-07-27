using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PRN232.Common.Grpc;
using PRN232.Plagiarism.Application.DTOs;
using PRN232.Plagiarism.Application.Interfaces;

namespace PRN232.Plagiarism.Infrastructure.Security;

public class GrpcAuthService : IAuthService
{
    private readonly AuthGrpcService.AuthGrpcServiceClient _client;
    private readonly ILogger<GrpcAuthService> _logger;

    public GrpcAuthService(AuthGrpcService.AuthGrpcServiceClient client, ILogger<GrpcAuthService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<UserClaimsDto> VerifyTokenAsync(string token)
    {
        try
        {
            var request = new VerifyTokenRequest { Token = token };
            var response = await _client.VerifyTokenAsync(request);

            return new UserClaimsDto
            {
                IsValid = response.IsValid,
                UserId = response.UserId,
                Username = response.Username,
                Role = response.Role
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify token over gRPC in Plagiarism service");
            return new UserClaimsDto { IsValid = false };
        }
    }
}
