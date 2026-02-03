using ClassroomService.Domain.DTOs;

namespace ClassroomService.Domain.Interfaces;

public interface IExcelService
{
    /// <summary>
    /// Imports student enrollments from Excel file
    /// </summary>
    Task<List<ImportStudentEnrollmentDto>> ImportStudentEnrollmentsFromExcelAsync(Stream excelStream);
    
    /// <summary>
    /// Generates a template Excel file for student enrollment imports
    /// </summary>
    byte[] GenerateStudentEnrollmentTemplate();
    
    /// <summary>
    /// Imports students for a specific course from Excel file
    /// </summary>
    Task<List<ImportCourseStudentsDto>> ImportCourseStudentsFromExcelAsync(Stream excelStream);
    
    /// <summary>
    /// Generates a template Excel file for course-specific student imports
    /// </summary>
    byte[] GenerateCourseStudentsTemplate();
    
    /// <summary>
    /// Exports data to Excel file
    /// </summary>
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName);
}