using System.Text;
using System.Text.Json;
using ClassroomService.Domain.Interfaces;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for calculating text differences using DiffPlex library
/// </summary>
public class ChangeTrackingService : IChangeTrackingService
{
    private readonly ILogger<ChangeTrackingService> _logger;
    private readonly Differ _differ;

    public ChangeTrackingService(ILogger<ChangeTrackingService> logger)
    {
        _logger = logger;
        _differ = new Differ();
    }

    public DiffResult CalculateDiff(string? oldContent, string? newContent)
    {
        var old = oldContent ?? string.Empty;
        var @new = newContent ?? string.Empty;
        
        var diffBuilder = new InlineDiffBuilder(_differ);
        var diff = diffBuilder.BuildDiffModel(old, @new);
        
        var result = new DiffResult();
        var operations = new List<ChangeOperation>();
        
        int lineNumber = 0;
        int position = 0;
        
        foreach (var line in diff.Lines)
        {
            lineNumber++;
            
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    result.LinesAdded++;
                    result.CharactersAdded += line.Text?.Length ?? 0;
                    operations.Add(new ChangeOperation
                    {
                        Type = ChangeOperationType.Insert,
                        Position = position,
                        Length = line.Text?.Length ?? 0,
                        NewText = line.Text,
                        LineNumber = lineNumber
                    });
                    break;
                    
                case ChangeType.Deleted:
                    result.LinesDeleted++;
                    result.CharactersDeleted += line.Text?.Length ?? 0;
                    operations.Add(new ChangeOperation
                    {
                        Type = ChangeOperationType.Delete,
                        Position = position,
                        Length = line.Text?.Length ?? 0,
                        OldText = line.Text,
                        LineNumber = lineNumber
                    });
                    break;
                    
                case ChangeType.Modified:
                    result.LinesModified++;
                    
                    // Count character-level changes within modified lines
                    if (line.SubPieces != null)
                    {
                        foreach (var piece in line.SubPieces)
                        {
                            if (piece.Type == ChangeType.Deleted)
                            {
                                result.CharactersDeleted += piece.Text?.Length ?? 0;
                            }
                            else if (piece.Type == ChangeType.Inserted)
                            {
                                result.CharactersAdded += piece.Text?.Length ?? 0;
                            }
                        }
                    }
                    
                    operations.Add(new ChangeOperation
                    {
                        Type = ChangeOperationType.Replace,
                        Position = position,
                        LineNumber = lineNumber,
                        OldText = line.Text,
                        NewText = line.Text,
                        Length = line.Text?.Length ?? 0
                    });
                    break;
            }
            
            position += (line.Text?.Length ?? 0) + 1; // +1 for newline
        }
        
        result.Operations = operations;
        
        _logger.LogDebug("📊 Diff calculated: +{Added} lines, -{Deleted} lines, ~{Modified} lines, +{CharsAdded} chars, -{CharsDeleted} chars",
            result.LinesAdded, result.LinesDeleted, result.LinesModified, result.CharactersAdded, result.CharactersDeleted);
        
        return result;
    }

    public string GenerateSummary(DiffResult diff)
    {
        var parts = new List<string>();
        
        if (diff.LinesAdded > 0)
            parts.Add($"+{diff.LinesAdded} line(s)");
        
        if (diff.LinesDeleted > 0)
            parts.Add($"-{diff.LinesDeleted} line(s)");
        
        if (diff.LinesModified > 0)
            parts.Add($"~{diff.LinesModified} line(s)");
        
        if (parts.Count == 0)
            return "No changes detected";
        
        var summary = string.Join(", ", parts) + $" | +{diff.CharactersAdded} chars, -{diff.CharactersDeleted} chars";
        
        _logger.LogDebug("📝 Generated summary: {Summary}", summary);
        
        return summary;
    }

    public string CreateUnifiedDiff(string? oldContent, string? newContent)
    {
        var old = oldContent ?? string.Empty;
        var @new = newContent ?? string.Empty;
        
        var diff = _differ.CreateLineDiffs(old, @new, false);
        
        var sb = new StringBuilder();
        sb.AppendLine("@@ -1,1 +1,1 @@");
        
        var oldLines = old.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var newLines = @new.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        
        foreach (var block in diff.DiffBlocks)
        {
            // Show deleted lines
            for (int i = block.DeleteStartA; i < block.DeleteStartA + block.DeleteCountA && i < oldLines.Length; i++)
            {
                sb.AppendLine($"-{oldLines[i]}");
            }
            
            // Show inserted lines
            for (int i = block.InsertStartB; i < block.InsertStartB + block.InsertCountB && i < newLines.Length; i++)
            {
                sb.AppendLine($"+{newLines[i]}");
            }
        }
        
        var unifiedDiff = sb.ToString();
        
        _logger.LogDebug("🔍 Unified diff created ({Length} characters)", unifiedDiff.Length);
        
        return unifiedDiff;
    }

    public string SerializeChangeOperations(DiffResult diff)
    {
        var operations = diff.Operations.Select(op => new
        {
            type = op.Type.ToString().ToLower(),
            position = op.Position,
            length = op.Length,
            oldText = op.OldText,
            newText = op.NewText,
            lineNumber = op.LineNumber
        });
        
        var json = JsonSerializer.Serialize(operations, new JsonSerializerOptions 
        { 
            WriteIndented = false 
        });
        
        _logger.LogDebug("🔄 Serialized {Count} change operations to JSON", diff.Operations.Count);
        
        return json;
    }
}
