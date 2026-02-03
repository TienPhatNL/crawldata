using ClassroomService.Domain.Entities;
using ClassroomService.Domain.Enums;
using ClassroomService.Domain.Interfaces;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Services;

public class TopicWeightHistoryService : ITopicWeightHistoryService
{
    private readonly ClassroomDbContext _context;
    private readonly ITermService _termService;
    
    public TopicWeightHistoryService(
        ClassroomDbContext context,
        ITermService termService)
    {
        _context = context;
        _termService = termService;
    }
    
    public async Task RecordCreationAsync(
        TopicWeight topicWeight, 
        Guid userId, 
        string? reason = null)
    {
        var affectedTerms = await _termService.GetAffectedTermNamesAsync(
            topicWeight.CourseCodeId, 
            topicWeight.SpecificCourseId);
        
        var affectedTermIds = await _termService.GetAffectedTermIdsAsync(
            topicWeight.CourseCodeId, 
            topicWeight.SpecificCourseId);
        
        // Get primary term for tracking
        Guid? primaryTermId = null;
        string? primaryTermName = null;
        
        if (topicWeight.SpecificCourseId.HasValue)
        {
            var courseTerm = await _termService.GetCourseTermAsync(topicWeight.SpecificCourseId.Value);
            primaryTermId = courseTerm?.Id;
            primaryTermName = courseTerm?.Name;
        }
        else if (affectedTermIds.Any())
        {
            primaryTermId = affectedTermIds.First();
            primaryTermName = affectedTerms.FirstOrDefault();
        }
        
        var history = new TopicWeightHistory
        {
            TopicWeightId = topicWeight.Id,
            TopicId = topicWeight.TopicId,
            CourseCodeId = topicWeight.CourseCodeId,
            SpecificCourseId = topicWeight.SpecificCourseId,
            TermId = primaryTermId,
            TermName = primaryTermName,
            OldWeightPercentage = null, // No old value for creation
            NewWeightPercentage = topicWeight.WeightPercentage,
            ModifiedBy = userId,
            ModifiedAt = DateTime.UtcNow,
            Action = WeightHistoryAction.Created,
            ChangeReason = reason ?? "Initial configuration",
            AffectedTerms = affectedTerms.Any() ? string.Join(", ", affectedTerms) : null
        };
        
        await _context.TopicWeightHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }
    
    public async Task RecordUpdateAsync(
        TopicWeight topicWeight, 
        decimal oldWeight, 
        Guid userId, 
        string? reason = null)
    {
        var affectedTerms = await _termService.GetAffectedTermNamesAsync(
            topicWeight.CourseCodeId, 
            topicWeight.SpecificCourseId);
        
        var affectedTermIds = await _termService.GetAffectedTermIdsAsync(
            topicWeight.CourseCodeId, 
            topicWeight.SpecificCourseId);
        
        // Get primary term for tracking
        Guid? primaryTermId = null;
        string? primaryTermName = null;
        
        if (topicWeight.SpecificCourseId.HasValue)
        {
            var courseTerm = await _termService.GetCourseTermAsync(topicWeight.SpecificCourseId.Value);
            primaryTermId = courseTerm?.Id;
            primaryTermName = courseTerm?.Name;
        }
        else if (affectedTermIds.Any())
        {
            primaryTermId = affectedTermIds.First();
            primaryTermName = affectedTerms.FirstOrDefault();
        }
        
        var history = new TopicWeightHistory
        {
            TopicWeightId = topicWeight.Id,
            TopicId = topicWeight.TopicId,
            CourseCodeId = topicWeight.CourseCodeId,
            SpecificCourseId = topicWeight.SpecificCourseId,
            TermId = primaryTermId,
            TermName = primaryTermName,
            OldWeightPercentage = oldWeight,
            NewWeightPercentage = topicWeight.WeightPercentage,
            ModifiedBy = userId,
            ModifiedAt = DateTime.UtcNow,
            Action = WeightHistoryAction.Updated,
            ChangeReason = reason ?? "Weight updated",
            AffectedTerms = affectedTerms.Any() ? string.Join(", ", affectedTerms) : null
        };
        
        await _context.TopicWeightHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }
    
    public async Task RecordDeletionAsync(
        TopicWeight topicWeight, 
        Guid userId, 
        string? reason = null)
    {
        var affectedTerms = await _termService.GetAffectedTermNamesAsync(
            topicWeight.CourseCodeId, 
            topicWeight.SpecificCourseId);
        
        var affectedTermIds = await _termService.GetAffectedTermIdsAsync(
            topicWeight.CourseCodeId, 
            topicWeight.SpecificCourseId);
        
        // Get primary term for tracking
        Guid? primaryTermId = null;
        string? primaryTermName = null;
        
        if (topicWeight.SpecificCourseId.HasValue)
        {
            var courseTerm = await _termService.GetCourseTermAsync(topicWeight.SpecificCourseId.Value);
            primaryTermId = courseTerm?.Id;
            primaryTermName = courseTerm?.Name;
        }
        else if (affectedTermIds.Any())
        {
            primaryTermId = affectedTermIds.First();
            primaryTermName = affectedTerms.FirstOrDefault();
        }
        
        var history = new TopicWeightHistory
        {
            TopicWeightId = topicWeight.Id,
            TopicId = topicWeight.TopicId,
            CourseCodeId = topicWeight.CourseCodeId,
            SpecificCourseId = topicWeight.SpecificCourseId,
            TermId = primaryTermId,
            TermName = primaryTermName,
            OldWeightPercentage = topicWeight.WeightPercentage,
            NewWeightPercentage = 0, // Weight removed
            ModifiedBy = userId,
            ModifiedAt = DateTime.UtcNow,
            Action = WeightHistoryAction.Deleted,
            ChangeReason = reason ?? "Configuration removed",
            AffectedTerms = affectedTerms.Any() ? string.Join(", ", affectedTerms) : null
        };
        
        await _context.TopicWeightHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }
    
    public async Task<List<TopicWeightHistory>> GetHistoryAsync(Guid topicWeightId)
    {
        return await _context.TopicWeightHistories
            .Include(h => h.Topic)
            .Include(h => h.CourseCode)
            .Include(h => h.SpecificCourse)
            .Include(h => h.Term)
            .Where(h => h.TopicWeightId == topicWeightId)
            .OrderByDescending(h => h.ModifiedAt)
            .ToListAsync();
    }
    
    public async Task<List<TopicWeightHistory>> GetAllHistoryAsync()
    {
        return await _context.TopicWeightHistories
            .Include(h => h.Topic)
            .Include(h => h.CourseCode)
            .Include(h => h.SpecificCourse)
            .Include(h => h.Term)
            .OrderByDescending(h => h.ModifiedAt)
            .Take(1000) // Limit for performance
            .ToListAsync();
    }
}
