using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PRN232.Plagiarism.Application.Services;

public class VsProjectGuidScanner
{
    private static readonly Regex CsprojGuidRegex = new Regex(
        @"<ProjectGuid>{?([0-9a-fA-F\-]+)}?</ProjectGuid>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SlnGuidRegex = new Regex(
        @"Project\(\""[^\""]+\""\)\s*=\s*\""[^\""]+\""\s*,\s*\""[^\""]+\""\s*,\s*\""\{([0-9a-fA-F\-]+)\}\""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public List<string> ScanProjectGuids(string workspacePath)
    {
        var guids = new List<string>();

        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return guids;
        }

        try
        {
            // Quét tất cả file .csproj
            var csprojFiles = Directory.GetFiles(workspacePath, "*.csproj", SearchOption.AllDirectories);
            foreach (var file in csprojFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var match = CsprojGuidRegex.Match(content);
                    if (match.Success)
                    {
                        var guidStr = match.Groups[1].Value.ToLower().Trim();
                        if (!guids.Contains(guidStr))
                        {
                            guids.Add(guidStr);
                        }
                    }
                }
                catch
                {
                    // Bỏ qua lỗi đọc file đơn lẻ
                }
            }

            // Quét tất cả file .sln
            var slnFiles = Directory.GetFiles(workspacePath, "*.sln", SearchOption.AllDirectories);
            foreach (var file in slnFiles)
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var matches = SlnGuidRegex.Matches(content);
                    foreach (Match match in matches)
                    {
                        if (match.Success)
                        {
                            var guidStr = match.Groups[1].Value.ToLower().Trim();
                            if (!guids.Contains(guidStr))
                            {
                                guids.Add(guidStr);
                            }
                        }
                    }
                }
                catch
                {
                    // Bỏ qua lỗi đọc file đơn lẻ
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VsProjectGuidScanner] Error scanning GUIDs: {ex.Message}");
        }

        return guids;
    }
}
