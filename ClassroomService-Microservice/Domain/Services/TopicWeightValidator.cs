using ClassroomService.Domain.Entities;

namespace ClassroomService.Domain.Services;

public class TopicWeightValidator
{
    /// <summary>
    /// Validates TopicWeight entity before save
    /// </summary>
    public ValidationResult ValidateTopicWeight(TopicWeight weight)
    {
        var errors = new List<string>();
        
        // Rule: Must have either CourseCodeId OR SpecificCourseId (not both, not neither)
        if (weight.CourseCodeId.HasValue && weight.SpecificCourseId.HasValue)
        {
            errors.Add("TopicWeight cannot have both CourseCodeId and SpecificCourseId set.");
        }
        
        if (!weight.CourseCodeId.HasValue && !weight.SpecificCourseId.HasValue)
        {
            errors.Add("TopicWeight must have either CourseCodeId or SpecificCourseId set.");
        }
        
        // Rule: Weight must be between 0 and 100
        if (weight.WeightPercentage < 0 || weight.WeightPercentage > 100)
        {
            errors.Add("Weight percentage must be between 0 and 100.");
        }
        
        return new ValidationResult(errors);
    }
}

public class ValidationResult
{
    public List<string> Errors { get; set; }
    public bool IsValid => !Errors.Any();
    public string ErrorMessage => string.Join("; ", Errors);
    
    public ValidationResult(List<string> errors)
    {
        Errors = errors ?? new List<string>();
    }
}
