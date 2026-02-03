-- Custom data migration script for CourseCode restructure
-- This script should be run BEFORE applying the EF migration

-- Step 1: Create a temporary CourseCode mapping table
CREATE TABLE #CourseMigrationMapping (
    OldCourseCode NVARCHAR(50),
    NewCourseCodeId UNIQUEIDENTIFIER
);

-- Step 2: Insert unique course codes into CourseCodes table (if not exists)
INSERT INTO [dbo].[CourseCodes] (
    [Id], [Code], [Title], [Description], [IsActive], [CreditHours], 
    [Department], [CreatedAt], [CreatedBy]
)
OUTPUT INSERTED.Code, INSERTED.Id INTO #CourseMigrationMapping
SELECT 
    NEWID() as Id,
    CourseCode as Code,
    CourseCode + ' Course' as Title,
    'Migrated from existing course data' as Description,
    1 as IsActive,
    3 as CreditHours,
    'General' as Department,
    GETUTCDATE() as CreatedAt,
    NULL as CreatedBy
FROM (
    SELECT DISTINCT CourseCode 
    FROM [dbo].[Courses] 
    WHERE CourseCode IS NOT NULL AND CourseCode != ''
) AS DistinctCodes
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[CourseCodes] cc WHERE cc.Code = DistinctCodes.CourseCode
);

-- Step 3: Update existing courses with CourseCodeId and set default values
UPDATE c 
SET 
    CourseCodeId = (SELECT Id FROM [dbo].[CourseCodes] cc WHERE cc.Code = c.CourseCode),
    [Description] = ISNULL(c.[Name], 'Course Description'),
    [Term] = 'Fall 2024',
    [Name] = cc.Code + ' - Unknown Lecturer' -- Will be updated by service later
FROM [dbo].[Courses] c
INNER JOIN [dbo].[CourseCodes] cc ON c.CourseCode = cc.Code
WHERE c.CourseCodeId IS NULL;

-- Clean up
DROP TABLE #CourseMigrationMapping;

-- Verify data integrity
SELECT 
    'Courses without CourseCodeId' as Issue,
    COUNT(*) as Count
FROM [dbo].[Courses] 
WHERE CourseCodeId IS NULL
UNION ALL
SELECT 
    'CourseCodes created' as Issue,
    COUNT(*) as Count
FROM [dbo].[CourseCodes];