using ClassroomService.Domain.Interfaces;
using ClassroomService.Domain.Entities;
using ClassroomService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClassroomService.Infrastructure.Services;

/// <summary>
/// Service for resolving topic weights and calculating weighted grades
/// </summary>
public class TopicWeightService : ITopicWeightService
{
    private readonly ClassroomDbContext _context;

    public TopicWeightService(ClassroomDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Resolve weight for a topic in a specific course
    /// Priority: Specific Course > CourseCode > 0
    /// Divides weight equally among all assignments with this topic
    /// </summary>
    public async Task<decimal> ResolveTopicWeightAsync(Guid topicId, Guid courseCodeId, Guid courseId, bool divideByAssignmentCount = true)
    {
        // Check for specific course override
        var specificWeight = await _context.TopicWeights
            .Where(tw => tw.TopicId == topicId && tw.SpecificCourseId == courseId)
            .Select(tw => (decimal?)tw.WeightPercentage)
            .FirstOrDefaultAsync();

        decimal topicWeight;
        if (specificWeight.HasValue)
        {
            topicWeight = specificWeight.Value;
        }
        else
        {
            // Use course code standard
            var courseCodeWeight = await _context.TopicWeights
                .Where(tw => tw.TopicId == topicId && tw.CourseCodeId == courseCodeId)
                .Select(tw => (decimal?)tw.WeightPercentage)
                .FirstOrDefaultAsync();

            topicWeight = courseCodeWeight ?? 0;
        }

        if (!divideByAssignmentCount || topicWeight == 0)
            return topicWeight;

        // Count assignments with this topic in this course
        var assignmentCount = await _context.Assignments
            .CountAsync(a => a.CourseId == courseId && a.TopicId == topicId);

        if (assignmentCount == 0)
            return topicWeight;

        // Divide weight equally among all assignments
        return topicWeight / assignmentCount;
    }

    /// <summary>
    /// Calculate weighted final grade for a student in a course
    /// </summary>
    public async Task<decimal> CalculateWeightedGradeAsync(Guid courseId, Guid studentId)
    {
        var course = await _context.Courses
            .Include(c => c.CourseCode)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            return 0;

        var assignments = await _context.Assignments
            .Where(a => a.CourseId == courseId)
            .Include(a => a.Reports)
            .ToListAsync();

        decimal totalWeightedScore = 0;
        decimal totalConfiguredWeight = 0;

        foreach (var assignment in assignments)
        {
            // Use snapshot weight (captured at assignment creation time)
            // This preserves historical accuracy even if TopicWeight is updated later
            if (!assignment.WeightPercentageSnapshot.HasValue)
                continue; // Skip assignments without configured weights
            
            decimal weight = assignment.WeightPercentageSnapshot.Value;

            if (weight == 0)
                continue; // Skip assignments with zero weight

            // Get best student submission score (individual or group)
            Report? bestReport = null;
            
            foreach (var report in assignment.Reports.Where(r => r.Grade.HasValue))
            {
                bool isStudentReport = false;
                
                // Check if this is the student's individual submission
                if (!report.IsGroupSubmission && report.SubmittedBy == studentId)
                {
                    isStudentReport = true;
                }
                // Check if this is a group submission where student is a member
                else if (report.IsGroupSubmission && report.GroupId.HasValue)
                {
                    var isGroupMember = await _context.GroupMembers
                        .AnyAsync(gm => gm.GroupId == report.GroupId.Value && 
                                       gm.Enrollment.StudentId == studentId);
                    
                    if (isGroupMember)
                    {
                        isStudentReport = true;
                    }
                }
                
                if (isStudentReport && (bestReport == null || report.Grade > bestReport.Grade))
                {
                    bestReport = report;
                }
            }

            if (bestReport == null || !assignment.MaxPoints.HasValue || assignment.MaxPoints.Value == 0)
                continue; // Skip if not graded yet - don't count weight for ungraded assignments

            // Only count weight for assignments that have been graded
            totalConfiguredWeight += weight;

            // Calculate normalized score (0-1) then multiply by weight
            var normalizedScore = (decimal)bestReport.Grade!.Value / assignment.MaxPoints.Value;
            totalWeightedScore += normalizedScore * weight;
        }

        // Return proportional grade if total configured weight is less than 100%
        // This handles cases where not all assignments are created yet
        if (totalConfiguredWeight == 0)
            return 0;

        // If total weight < 100%, scale the grade proportionally
        // If total weight = 100%, this just returns the score as-is
        return (totalWeightedScore / totalConfiguredWeight) * 100;
    }
}
