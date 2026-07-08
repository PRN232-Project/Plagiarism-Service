using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PRN232.Plagiarism.Application.DTOs;
using PRN232.Plagiarism.Application.Interfaces;

namespace PRN232.Plagiarism.Application.Services;

public class RoslynPlagiarismScanner : IPlagiarismScanner
{
    public async Task<List<PlagiarismViolation>> ScanAsync(string workspacePath, List<string> bannedKeywords)
    {
        var violations = new List<PlagiarismViolation>();

        System.Console.WriteLine($"[Plagiarism Scanner] Starting scan for path: {workspacePath}");
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            System.Console.WriteLine($"[Plagiarism Scanner] Error: Path does not exist or is empty.");
            return violations;
        }

        if (bannedKeywords == null || bannedKeywords.Count == 0)
        {
            System.Console.WriteLine($"[Plagiarism Scanner] Error: Banned keywords list is empty.");
            return violations;
        }

        System.Console.WriteLine($"[Plagiarism Scanner] Banned keywords: {string.Join(", ", bannedKeywords)}");

        // Quét tất cả các file .cs trong thư mục workspace
        var csFiles = Directory.GetFiles(workspacePath, "*.cs", SearchOption.AllDirectories);
        System.Console.WriteLine($"[Plagiarism Scanner] Found {csFiles.Length} .cs files total.");

        foreach (var file in csFiles)
        {
            if (IsGeneratedOrBuildArtifact(workspacePath, file))
            {
                System.Console.WriteLine($"[Plagiarism Scanner] Skipped generated/artifact file: {Path.GetFileName(file)}");
                continue;
            }

            System.Console.WriteLine($"[Plagiarism Scanner] Scanning file: {file}");
            try
            {
                var code = await File.ReadAllTextAsync(file);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = await tree.GetRootAsync();
                var fileName = Path.GetFileName(file);

                var walker = new BannedKeywordWalker(fileName, code, bannedKeywords);
                walker.Visit(root);

                if (walker.Violations.Count > 0)
                {
                    System.Console.WriteLine($"[Plagiarism Scanner] Found {walker.Violations.Count} violations in {fileName}");
                }
                violations.AddRange(walker.Violations);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Plagiarism Scanner] Exception scanning file {file}: {ex.Message}");
            }
        }

        System.Console.WriteLine($"[Plagiarism Scanner] Scan completed. Total violations found: {violations.Count}");
        return violations;
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

    private class BannedKeywordWalker : CSharpSyntaxWalker
    {
        private readonly string _fileName;
        private readonly string _code;
        private readonly string[] _lines;
        private readonly List<string> _bannedKeywords;
        
        public List<PlagiarismViolation> Violations { get; } = new();

        public BannedKeywordWalker(string fileName, string code, List<string> bannedKeywords)
        {
            _fileName = fileName;
            _code = code;
            _lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            _bannedKeywords = bannedKeywords;
        }

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            var namespaceImport = node.Name?.ToString() ?? string.Empty;
            foreach (var keyword in _bannedKeywords)
            {
                if (namespaceImport.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    AddViolation(node, keyword);
                }
            }
            base.VisitUsingDirective(node);
        }

        public override void VisitIdentifierName(IdentifierNameSyntax node)
        {
            var identifierText = node.Identifier.Text;
            
            // Chỉ kiểm tra khi nó không nằm trong using directive để tránh trùng lặp cảnh báo
            if (!IsInsideUsingDirective(node))
            {
                foreach (var keyword in _bannedKeywords)
                {
                    if (identifierText.Equals(keyword, StringComparison.OrdinalIgnoreCase) || 
                        identifierText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        AddViolation(node, keyword);
                    }
                }
            }
            base.VisitIdentifierName(node);
        }

        private void AddViolation(SyntaxNode node, string keyword)
        {
            var location = node.GetLocation();
            var lineSpan = location.GetLineSpan();
            var zeroBasedLine = lineSpan.StartLinePosition.Line;
            var lineNumber = zeroBasedLine + 1;

            var snippet = (zeroBasedLine >= 0 && zeroBasedLine < _lines.Length) 
                ? _lines[zeroBasedLine].Trim() 
                : node.ToString();

            // Tránh thêm trùng lặp cùng một dòng vi phạm cùng một từ khóa trong cùng một file
            var isDuplicate = Violations.Any(v => 
                v.FileName == _fileName && 
                v.LineNumber == lineNumber && 
                v.BannedKeyword.Equals(keyword, StringComparison.OrdinalIgnoreCase));

            if (!isDuplicate)
            {
                Violations.Add(new PlagiarismViolation(_fileName, keyword, lineNumber, snippet));
            }
        }

        private static bool IsInsideUsingDirective(SyntaxNode node)
        {
            var current = node.Parent;
            while (current != null)
            {
                if (current is UsingDirectiveSyntax)
                {
                    return true;
                }
                current = current.Parent;
            }
            return false;
        }
    }
}
