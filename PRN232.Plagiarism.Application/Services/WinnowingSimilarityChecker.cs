using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PRN232.Plagiarism.Application.Services;

public class WinnowingSimilarityChecker
{
    private const int K = 8;  // K-gram size
    private const int W = 4;  // Window size
    private const int MOD = 1000000007; // Prime modulus for hashing

    public HashSet<int> GetFingerprints(string workspacePath)
    {
        var fingerprints = new HashSet<int>();

        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return fingerprints;
        }

        try
        {
            var csFiles = Directory.GetFiles(workspacePath, "*.cs", SearchOption.AllDirectories);
            var allNormalizedTokens = new System.Text.StringBuilder();

            foreach (var file in csFiles)
            {
                if (IsGeneratedOrBuildArtifact(workspacePath, file))
                {
                    continue;
                }

                try
                {
                    var content = File.ReadAllText(file);
                    var normalized = NormalizeCode(content);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        allNormalizedTokens.Append(normalized);
                    }
                }
                catch
                {
                    // Bỏ qua lỗi đọc file đơn lẻ
                }
            }

            var tokenString = allNormalizedTokens.ToString();
            if (tokenString.Length >= K)
            {
                var hashes = GenerateKgramHashes(tokenString);
                fingerprints = PerformWinnowing(hashes);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WinnowingSimilarityChecker] Error: {ex.Message}");
        }

        return fingerprints;
    }

    public decimal CalculateSimilarity(HashSet<int> fingerprintsA, HashSet<int> fingerprintsB)
    {
        if (fingerprintsA == null || fingerprintsB == null || fingerprintsA.Count == 0 || fingerprintsB.Count == 0)
        {
            return 0;
        }

        var intersection = fingerprintsA.Intersect(fingerprintsB).Count();
        var union = fingerprintsA.Union(fingerprintsB).Count();

        if (union == 0) return 0;

        var score = (decimal)intersection / union;
        return Math.Round(score, 4); // Làm tròn 4 chữ số thập phân
    }

    private string NormalizeCode(string code)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();
            var tokens = root.DescendantTokens();
            var sb = new System.Text.StringBuilder();

            foreach (var token in tokens)
            {
                var kind = token.Kind();
                
                if (token.IsKeyword())
                {
                    sb.Append(token.Text);
                }
                else if (kind == SyntaxKind.IdentifierToken)
                {
                    sb.Append("V"); // Chuẩn hóa tất cả tên định danh thành 'V'
                }
                else if (kind == SyntaxKind.NumericLiteralToken || kind == SyntaxKind.StringLiteralToken || kind == SyntaxKind.CharacterLiteralToken)
                {
                    sb.Append("L"); // Chuẩn hóa các hằng số/chuỗi literal thành 'L'
                }
                else
                {
                    // Các dấu ngoặc, phép toán, dấu chấm phẩy giữ nguyên cấu trúc
                    sb.Append(token.Text);
                }
            }

            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private List<int> GenerateKgramHashes(string text)
    {
        var hashes = new List<int>();
        var currentHash = 0L;
        var power = 1L;

        // Tính số mũ lớn nhất
        for (int i = 0; i < K - 1; i++)
        {
            power = (power * 31) % MOD;
        }

        // Tính hash cho k-gram đầu tiên
        for (int i = 0; i < K; i++)
        {
            currentHash = (currentHash * 31 + text[i]) % MOD;
        }
        hashes.Add((int)currentHash);

        // Sử dụng Rolling Hash Rabin-Karp cho các k-grams tiếp theo
        for (int i = 1; i <= text.Length - K; i++)
        {
            currentHash = (currentHash - text[i - 1] * power) % MOD;
            if (currentHash < 0) currentHash += MOD;

            currentHash = (currentHash * 31 + text[i + K - 1]) % MOD;
            hashes.Add((int)currentHash);
        }

        return hashes;
    }

    private HashSet<int> PerformWinnowing(List<int> hashes)
    {
        var fingerprints = new HashSet<int>();
        if (hashes.Count < W)
        {
            // Nếu quá ngắn, lấy hash nhỏ nhất làm vân tay
            fingerprints.Add(hashes.Min());
            return fingerprints;
        }

        for (int i = 0; i <= hashes.Count - W; i++)
        {
            var minVal = int.MaxValue;
            var minPos = -1;

            // Tìm giá trị hash nhỏ nhất trong cửa sổ cỡ W
            for (int j = 0; j < W; j++)
            {
                var val = hashes[i + j];
                if (val < minVal)
                {
                    minVal = val;
                    minPos = i + j;
                }
            }

            if (minPos != -1)
            {
                fingerprints.Add(minVal);
            }
        }

        return fingerprints;
    }

    private static bool IsGeneratedOrBuildArtifact(string workspacePath, string filePath)
    {
        var relativePath = Path.GetRelativePath(workspacePath, filePath);
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return segments.Contains("bin", StringComparer.OrdinalIgnoreCase)
               || segments.Contains("obj", StringComparer.OrdinalIgnoreCase)
               || segments.Contains("Migrations", StringComparer.OrdinalIgnoreCase)
               || filePath.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)
               || filePath.EndsWith("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase);
    }
}
