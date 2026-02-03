using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Enums;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClassroomService.Infrastructure.Services;

public class TopicWeightValidationService : ITopicWeightValidationService
{
    private readonly ClassroomDbContext _context;
    private readonly ITermService _termService;
    private readonly ILogger<TopicWeightValidationService> _logger;
    
    public TopicWeightValidationService(
        ClassroomDbContext context,
        ITermService termService,
        ILogger<TopicWeightValidationService> logger)
    {
        _context = context;
        _termService = termService;
        _logger = logger;
    }
    
    public async Task<TopicWeightValidationResult> ValidateUpdateAsync(Guid topicWeightId)
    {
        var topicWeight = await _context.TopicWeights
            .Include(tw => tw.CourseCode)
            .Include(tw => tw.SpecificCourse)
            .FirstOrDefaultAsync(tw => tw.Id == topicWeightId);
        
        if (topicWeight == null)
            return TopicWeightValidationResult.Forbidden("TopicWeight not found", new());
        
        // NEW RULE: Only block if ACTIVE term exists (past terms are safe due to WeightPercentageSnapshot)
        bool hasActiveTerm;
        List<string> affectedTerms;
        
        if (topicWeight.CourseCodeId.HasValue)
        {
            // CourseCode-level weight: Check if ANY course with this code is in an ACTIVE term
            hasActiveTerm = await _termService.HasActiveTermForCourseCodeAsync(
                topicWeight.CourseCodeId.Value);
            affectedTerms = await _termService.GetAffectedTermNamesAsync(
                topicWeight.CourseCodeId, null);
        }
        else if (topicWeight.SpecificCourseId.HasValue)
        {
            // Course-specific weight: Check if THIS course is in an ACTIVE term
            hasActiveTerm = await _termService.HasActiveTermForCourseAsync(
                topicWeight.SpecificCourseId.Value);
            affectedTerms = await _termService.GetAffectedTermNamesAsync(
                null, topicWeight.SpecificCourseId);
        }
        else
        {
            return TopicWeightValidationResult.Forbidden(
                "TopicWeight must have either CourseCodeId or SpecificCourseId", new());
        }
        
        if (hasActiveTerm)
        {
            var termList = string.Join(", ", affectedTerms);
            return TopicWeightValidationResult.Forbidden(
                $"Cannot update: Weight is being used in active term(s): {termList}. " +
                $"Past terms are protected by assignment snapshots. Please wait until active term ends.",
                affectedTerms);
        }
        
        // Check for future terms (warning only)
        var futureCourses = await GetFutureCoursesCountAsync(
            topicWeight.CourseCodeId, 
            topicWeight.SpecificCourseId);
        
        if (futureCourses > 0)
        {
            return TopicWeightValidationResult.Warning(
                $"This change will affect {futureCourses} upcoming course(s).",
                new());
        }
        
        return TopicWeightValidationResult.Success();
    }
    
    public async Task<TopicWeightValidationResult> ValidateDeleteAsync(Guid topicWeightId)
    {
        // Same logic as update - can't delete during active term
        var updateValidation = await ValidateUpdateAsync(topicWeightId);
        
        if (!updateValidation.IsValid)
        {
            return TopicWeightValidationResult.Forbidden(
                updateValidation.ErrorMessage?.Replace("update", "delete") ?? "Cannot delete",
                updateValidation.AffectedTerms);
        }
        
        return TopicWeightValidationResult.Success();
    }
    
    public async Task<TopicWeightValidationResult> ValidateCreateAsync(
        Guid? courseCodeId, 
        Guid? specificCourseId)
    {
        // NEW RULE: Only block creation for ACTIVE terms (past terms are safe with snapshots)
        if (courseCodeId.HasValue)
        {
            var hasActiveTerm = await _termService.HasActiveTermForCourseCodeAsync(
                courseCodeId.Value);
            
            if (hasActiveTerm)
            {
                var terms = await _termService.GetAffectedTermNamesAsync(courseCodeId, null);
                return TopicWeightValidationResult.Forbidden(
                    $"Cannot create: CourseCode is being used in active term(s): {string.Join(", ", terms)}. " +
                    $"Past terms are protected by assignment snapshots. Please wait until active term ends.",
                    terms);
            }
        }
        else if (specificCourseId.HasValue)
        {
            var hasActiveTerm = await _termService.HasActiveTermForCourseAsync(
                specificCourseId.Value);
            
            if (hasActiveTerm)
            {
                var terms = await _termService.GetAffectedTermNamesAsync(null, specificCourseId);
                return TopicWeightValidationResult.Forbidden(
                    $"Cannot create: Course is in an active term ({string.Join(", ", terms)}). " +
                    $"Past terms are protected by assignment snapshots. Please wait until active term ends.",
                    terms);
            }
        }
        
        return TopicWeightValidationResult.Success();
    }
    
    /// <summary>
    /// Validate if a TopicWeight operation can be performed for a specific course
    /// Special rule: Bypasses term validation if course status is PendingApproval
    /// This allows course setup even during active terms
    /// </summary>
    public async Task<TopicWeightValidationResult> ValidateForCourseOperationAsync(
        Guid courseId, 
        TopicWeightOperation operation)
    {
        // Get course with term
        var course = await _context.Courses
            .Include(c => c.Term)
            .FirstOrDefaultAsync(c => c.Id == courseId);
        
        if (course == null)
        {
            return TopicWeightValidationResult.Forbidden("Course not found", new());
        }
        
        // SPECIAL RULE: If course is PendingApproval, bypass term validation
        // This allows initial course setup even during active terms
        if (course.Status == CourseStatus.PendingApproval)
        {
            _logger.LogInformation(
                "Allowing {Operation} for course {CourseId} - Status is PendingApproval (bypassing term validation)",
                operation, courseId);
            
            return TopicWeightValidationResult.Success();
        }
        
        // For other statuses (Active, Inactive, Rejected), apply normal term validation
        var hasActiveTerm = await _termService.HasActiveTermForCourseAsync(courseId);
        
        if (hasActiveTerm)
        {
            var terms = await _termService.GetAffectedTermNamesAsync(null, courseId);
            var operationText = operation.ToString().ToLower();
            
            _logger.LogWarning(
                "Blocked {Operation} for course {CourseId} - Course is in active term(s): {Terms}",
                operation, courseId, string.Join(", ", terms));
            
            return TopicWeightValidationResult.Forbidden(
                $"Cannot {operationText}: Course is in an active term ({string.Join(", ", terms)}). " +
                $"Weight modifications are only allowed during PendingApproval status or when term is not active.",
                terms);
        }
        
        return TopicWeightValidationResult.Success();
    }
    
    private async Task<int> GetFutureCoursesCountAsync(Guid? courseCodeId, Guid? specificCourseId)
    {
        var now = DateTime.UtcNow;
        
        if (courseCodeId.HasValue)
        {
            return await _context.Courses
                .Include(c => c.Term)
                .CountAsync(c => c.CourseCodeId == courseCodeId.Value &&
                                c.Term.StartDate > now);
        }
        else if (specificCourseId.HasValue)
        {
            return await _context.Courses
                .Include(c => c.Term)
                .CountAsync(c => c.Id == specificCourseId.Value &&
                                c.Term.StartDate > now);
        }
        
        return 0;
    }
}
