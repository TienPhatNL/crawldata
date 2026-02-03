using FluentValidation;

namespace ClassroomService.Application.Features.Enrollments.Commands;

public class ImportCourseStudentsCommandValidator : AbstractValidator<ImportCourseStudentsCommand>
{
    public ImportCourseStudentsCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .NotEmpty().WithMessage("Course ID is required");

        RuleFor(x => x.ExcelFile)
            .NotNull().WithMessage("Excel file is required");

        RuleFor(x => x.ImportedBy)
            .NotEmpty().WithMessage("Imported by user ID is required");

        When(x => x.ExcelFile != null, () =>
        {
            RuleFor(x => x.ExcelFile.Length)
                .GreaterThan(0).WithMessage("Excel file cannot be empty");

            RuleFor(x => x.ExcelFile.FileName)
                .Must(BeValidExcelFile).WithMessage("File must be an Excel file (.xlsx or .xls)");
        });
    }

    private static bool BeValidExcelFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".xlsx" || extension == ".xls";
    }
}