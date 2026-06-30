# Use .NET 9 SDK as build environment
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["PRN232.Plagiarism.Api/PRN232.Plagiarism.Api.csproj", "PRN232.Plagiarism.Api/"]
COPY ["PRN232.Plagiarism.Application/PRN232.Plagiarism.Application.csproj", "PRN232.Plagiarism.Application/"]
COPY ["PRN232.Plagiarism.Infrastructure/PRN232.Plagiarism.Infrastructure.csproj", "PRN232.Plagiarism.Infrastructure/"]
COPY ["PRN232.Plagiarism.Domain/PRN232.Plagiarism.Domain.csproj", "PRN232.Plagiarism.Domain/"]

# Restore dependencies
RUN dotnet restore "PRN232.Plagiarism.Api/PRN232.Plagiarism.Api.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish the API project
WORKDIR "/src/PRN232.Plagiarism.Api"
RUN dotnet publish "PRN232.Plagiarism.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use .NET 9 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose HTTP API port
EXPOSE 8080

ENTRYPOINT ["dotnet", "PRN232.Plagiarism.Api.dll"]


