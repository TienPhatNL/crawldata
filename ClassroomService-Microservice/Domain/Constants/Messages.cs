namespace ClassroomService.Domain.Constants;

/// <summary>
/// Contains all constant messages used throughout the ClassroomService application
/// </summary>
public static class Messages
{
    /// <summary>
    /// Success messages
    /// </summary>
    public static class Success
    {
        // CourseCode success messages
        public const string CourseCodeCreated = "Course code created successfully";
        public const string CourseCodeUpdated = "Course code updated successfully";
        public const string CourseCodeDeleted = "Course code deleted successfully";
        public const string CourseCodeRetrieved = "Course code retrieved successfully";
        public const string CourseCodesRetrieved = "Course codes retrieved successfully";

        // Course success messages
        public const string CourseCreated = "Course created successfully";
        public const string CourseUpdated = "Course updated successfully";
        public const string CourseDeleted = "Course deleted successfully";
        public const string CourseRetrieved = "Course retrieved successfully";
        public const string CoursesRetrieved = "Courses retrieved successfully";
        public const string CourseFound = "Course found";

        // Access Code messages
        public const string AccessCodeUpdated = "Access code updated successfully";
        public const string AccessCodeDisabled = "Access code requirement disabled";
        public const string AccessCodeRetrieved = "Access code retrieved successfully";
        public const string AccessCodeValidated = "Access code validated successfully";

        // Enrollment messages
        public const string StudentEnrolled = "Student enrolled successfully";
        public const string StudentUnenrolled = "Student unenrolled successfully";
        public const string SelfUnenrolled = "Successfully unenrolled from course";
        public const string EnrollmentStatusRetrieved = "Enrollment status retrieved successfully";
        public const string EnrollmentsRetrieved = "Course enrollments retrieved successfully";
        public const string EnrolledCoursesRetrieved = "Successfully retrieved {0} enrolled courses";
        public const string NoEnrolledCoursesFound = "No active enrolled courses found";

        // Join Course messages
        public const string CourseJoinedWithCode = "Successfully joined {0}!";
        public const string CourseJoinInfoRetrieved = "Course information retrieved successfully";

        // Statistics messages
        public const string StatisticsRetrieved = "Course statistics retrieved successfully";

        // Group messages
        public const string GroupCreated = "Group created successfully";
        public const string GroupUpdated = "Group updated successfully";
        public const string GroupDeleted = "Group deleted successfully";
        public const string GroupRetrieved = "Group retrieved successfully";
        public const string GroupsRetrieved = "Groups retrieved successfully";
        public const string GroupsRandomized = "Students randomized into groups successfully";
        public const string GroupAssignmentAssigned = "Assignment assigned to group successfully";

        // GroupMember messages
        public const string MemberAdded = "Member added to group successfully";
        public const string MemberRemoved = "Member removed from group successfully";
        public const string MembersAdded = "{0} members added to group successfully";
        public const string MembersAddedBulk = "{0} of {1} students added to group successfully";
        public const string GroupsRandomizedSuccess = "Successfully randomized {0} students into {1} groups";
        public const string LeaderAssigned = "Leader assigned successfully";
        public const string MembersRetrieved = "Group members retrieved successfully";
        public const string LeaderSet = "Group leader set successfully";
        public const string MemberRoleUpdated = "Member role updated successfully";
        public const string JoinedGroup = "Successfully joined group";
        public const string LeftGroup = "Successfully left group";
        public const string StudentGroupsRetrieved = "Student groups retrieved successfully";

        // General messages
        public const string OperationCompleted = "Operation completed successfully";
        public const string DataRetrieved = "Data retrieved successfully";
        public const string RequestProcessed = "Request processed successfully";

        // Assignment success messages
        public const string AssignmentCreated = "Assignment created successfully";
        public const string AssignmentUpdated = "Assignment updated successfully";
        public const string AssignmentDeleted = "Assignment deleted successfully";
        public const string AssignmentRetrieved = "Assignment retrieved successfully";
        public const string AssignmentsRetrieved = "Assignments retrieved successfully";
        public const string AssignmentClosed = "Assignment closed successfully";
        public const string DueDateExtended = "Due date extended successfully";
        public const string GroupsAssignedToAssignment = "Successfully assigned {0} group(s) to assignment";
        public const string GroupsUnassignedFromAssignment = "Successfully unassigned {0} group(s) from assignment";
        public const string AssignmentStatisticsRetrieved = "Statistics retrieved successfully";
        public const string AssignmentGroupsRetrieved = "Assignment groups retrieved successfully";
        public const string GroupAssignmentRetrieved = "Group assignment retrieved successfully";
        public const string NoAssignmentForGroup = "Group has no assignment assigned";
    }

    /// <summary>
    /// Error messages
    /// </summary>
    public static class Error
    {
        // General errors
        public const string InternalServerError = "An internal server error occurred";
        public const string NotFound = "Resource not found";

        // CourseCode errors
        public const string CourseCodeNotFound = "Course code not found";
        public const string CourseCodeExists = "A course code with code '{0}' already exists. Please choose a different course code.";
        public const string CourseCodeInUse = "Cannot delete course code '{0}' as it is currently being used by active courses";
        public const string CourseCodeInactive = "Course code '{0}' is inactive and cannot be used for new courses";
        public const string CourseCodeCreationFailed = "Error creating course code: {0}";
        public const string CourseCodeUpdateFailed = "Error updating course code: {0}";
        public const string CourseCodeDeletionFailed = "Error deleting course code: {0}";
        public const string CourseCodeRetrievalFailed = "Error retrieving course code: {0}";
        public const string CourseCodesRetrievalFailed = "Error retrieving course codes: {0}";
        public const string InvalidCourseCode = "Invalid course code. Please select a valid course code.";

        // Course errors (updated)
        public const string CourseNotFound = "Course not found";
        public const string CourseExists = "A course with code '{0}' already exists. Please choose a different course code.";
        public const string CourseCreationFailed = "Error creating course: {0}";
        public const string CourseUpdateFailed = "Error updating course: {0}";
        public const string CourseDeletionFailed = "Error deleting course: {0}";
        public const string CourseRetrievalFailed = "Error retrieving course: {0}";
        public const string CoursesRetrievalFailed = "Error retrieving courses: {0}";
        public const string CourseWithEnrollments = "Cannot delete course with enrolled students. Please remove all enrollments first.";

        // User/Lecturer errors
        public const string LecturerNotFound = "Invalid lecturer. The specified user was not found.";
        public const string InvalidLecturerRole = "Invalid lecturer. User must have Lecturer role.";
        public const string StudentNotFound = "Invalid student. User must exist and have Student role.";
        public const string UserIdNotFound = "User ID not found in token";
        public const string InvalidUser = "Invalid user. User must exist and have the correct role.";

        // Access Code errors
        public const string AccessCodeRequired = "This course requires an access code to join. Please provide the access code.";
        public const string InvalidAccessCode = "Invalid or expired access code. Please check the code and try again.";
        public const string CustomAccessCodeRequired = "Custom access code is required when AccessCodeType is Custom.";
        public const string InvalidAccessCodeFormat = "Invalid custom access code format.";
        public const string AccessCodeExpired = "Access code has expired.";
        public const string RateLimitExceeded = "Too many failed access code attempts. Please try again later.";
        public const string AccessCodeRetrievalFailed = "Error retrieving access code: {0}";
        public const string AccessCodeUpdateFailed = "Error updating access code: {0}";
        public const string OnlyOwnCourseAccess = "You can only view access codes for your own courses";
        public const string OnlyOwnCourseModify = "You can only modify access codes for your own courses";

        // Enrollment errors
        public const string AlreadyEnrolled = "You are already enrolled in this course.";
        public const string NotEnrolled = "You are not enrolled in this course.";
        public const string NotEnrolledInCourse = "Student is not enrolled in this course";
        public const string EnrollmentFailed = "Error enrolling student: {0}";
        public const string UnenrollmentFailed = "Error unenrolling student: {0}";
        public const string SelfUnenrollmentFailed = "Error unenrolling from course: {0}";
        public const string EnrollmentNotFound = "Enrollment not found";
        public const string EnrollmentRetrievalFailed = "Error retrieving enrollment status: {0}";
        public const string EnrollmentsRetrievalFailed = "Error retrieving course enrollments: {0}";
        public const string JoinCourseFailed = "Error joining course: {0}";
        public const string CourseNotAvailableForEnrollment = "Course not found or not available for enrollment";
        public const string OnlyActiveCourseEnrollment = "Only active courses allow enrollment";

        // Group errors
        public const string GroupNotFound = "Group not found";
        public const string GroupCreationFailed = "Error creating group: {0}";
        public const string GroupUpdateFailed = "Error updating group: {0}";
        public const string GroupDeletionFailed = "Error deleting group: {0}";
        public const string GroupRetrievalFailed = "Error retrieving group: {0}";
        public const string GroupsRetrievalFailed = "Error retrieving groups: {0}";
        public const string GroupLocked = "Group is locked and cannot be modified";
        public const string GroupFull = "Group has reached maximum capacity";
        public const string GroupNameExists = "A group with this name already exists in this course";
        public const string GroupAssignedToAssignment = "Cannot delete group that has been assigned to an assignment. Please unassign the group first.";
        public const string RandomizeGroupsFailed = "Error randomizing groups: {0}";
        public const string AssignGroupToAssignmentFailed = "Error assigning group to assignment: {0}";
        public const string NoEnrolledStudents = "No enrolled students found in this course";
        public const string InvalidGroupCount = "Invalid group count. Must be at least 1.";
        public const string InvalidGroupSize = "Invalid group size. Must be at least 1.";
        public const string AssignmentNotInCourse = "Assignment does not belong to the same course as the group";
        public const string AssignmentNotFound = "Assignment not found";

        // GroupMember errors
        public const string MemberNotFound = "Group member not found";
        public const string MemberAddFailed = "Error adding member to group: {0}";
        public const string MemberRemoveFailed = "Error removing member from group: {0}";
        public const string MemberAlreadyInGroup = "Student is already a member of this group";
        public const string MemberNotInGroup = "Student is not a member of this group";
        public const string LeaderSetFailed = "Error setting group leader: {0}";
        public const string NewLeaderNotInGroup = "Student is not a member of this group";
        public const string StudentNotEnrolledInCourse = "Student {0} is not enrolled in this course";
        public const string StudentAlreadyInAnotherGroup = "Student {0} is already in another group in this course";
        public const string NoAvailableStudentsForRandomization = "No available students found for randomization (all students are already in groups)";
        public const string NotEnoughStudentsForGroupSize = "Not enough available students ({0}) for requested group size ({1})";
        public const string MemberRoleUpdateFailed = "Error updating member role: {0}";
        public const string MembersRetrievalFailed = "Error retrieving group members: {0}";
        public const string LeaderRequired = "Group must have at least one leader";
        public const string OnlyLecturerCanManageGroups = "Only the course lecturer can manage groups";


        // Authorization errors
        public const string Unauthorized = "Unauthorized access";
        public const string Forbidden = "You do not have permission to perform this action";
        public const string OnlyOwnCourses = "You can only access your own courses";
        public const string OnlyOwnEnrollments = "You can only access your own enrollments";
        public const string AdminOrLecturerRequired = "Admin or lecturer access required";
        public const string LecturerRequired = "Lecturer access required";
        public const string StudentRequired = "Student access required";

        // Assignment errors
        public const string AssignmentCreationFailed = "Error creating assignment: {0}";
        public const string AssignmentUpdateFailed = "Error updating assignment: {0}";
        public const string AssignmentDeletionFailed = "Error deleting assignment: {0}";
        public const string AssignmentRetrievalFailed = "Error retrieving assignment: {0}";
        public const string AssignmentsRetrievalFailed = "Error retrieving assignments: {0}";
        public const string CannotUpdateClosedAssignment = "Cannot update assignment in Closed status";
        public const string CannotUpdateDatesForActiveAssignment = "Cannot update StartDate or DueDate for assignments in {0} status. Use ExtendDueDate endpoint to extend the due date.";
        public const string CannotDeleteNonDraftAssignment = "Can only delete assignments in Draft status";
        public const string CannotCloseDraftAssignment = "Cannot close an assignment in Draft status";
        public const string AssignmentAlreadyClosed = "Assignment is already closed";
        public const string ExtendedDueDateMustBeAfterDueDate = "Extended due date must be after the original due date";
        public const string DueDateExtensionFailed = "Error extending due date: {0}";
        public const string AssignmentCloseFailed = "Error closing assignment: {0}";
        public const string CannotAssignGroupsToIndividualAssignment = "Cannot assign groups to an individual assignment. Set IsGroupAssignment to true.";
        public const string OneOrMoreGroupsNotFound = "One or more groups not found";
        public const string GroupsMustBelongToSameCourse = "All groups must belong to the same course as the assignment";
        public const string GroupsAlreadyHaveAssignments = "The following groups already have assignments: {0}";
        public const string AssignGroupsFailed = "Error assigning groups: {0}";
        public const string UnassignGroupsFailed = "Error unassigning groups: {0}";
        public const string AssignmentStatisticsFailed = "Error retrieving statistics: {0}";
        public const string NoAssignmentsFound = "No assignments found for this course";
        public const string NoEnrolledCoursesForAssignments = "No enrolled courses found";

        // Validation errors
        public const string InvalidRequest = "Invalid request parameters";
        public const string ValidationFailed = "Validation failed";
        public const string RequiredFieldMissing = "Required field is missing: {0}";
        public const string InvalidFormat = "Invalid format for field: {0}";
        public const string InvalidValue = "Invalid value for field: {0}";
    }

    /// <summary>
    /// Information messages
    /// </summary>
    public static class Info
    {
        // Course info
        public const string NoCoursesFound = "No courses found matching the criteria";
        public const string NoCoursesForLecturer = "No courses found for this lecturer";
        public const string NoEnrolledCourses = "No active enrolled courses found for this student";
        public const string CourseRequiresAccessCode = "This course requires an access code to join";

        // Enrollment info
        public const string NoEnrollmentsFound = "No enrollments found for this course";
        public const string NoActiveEnrollments = "No active enrollments found";

        // Group info
        public const string NoGroupsFound = "No groups found for this course";
        public const string NoMembersFound = "No members found in this group";
        public const string NoAvailableGroups = "No available groups found for joining";
        public const string NoStudentGroups = "Student is not a member of any groups";

        // General info
        public const string NoDataFound = "No data found";
        public const string EmptyResult = "No results found for the specified criteria";

        // Assignment info
        public const string NoAssignmentsForCourse = "No assignments found for this course";
        public const string NoGroupsAssignedToAssignment = "No groups assigned to this assignment";
        public const string NoUnassignedGroupsInCourse = "No unassigned groups found in this course";
    }

    /// <summary>
    /// Notification and logging messages
    /// </summary>
    public static class Logging
    {
        // Course logging
        public const string CourseCreating = "Creating course with CourseCode: {0}, Name: {1}, LecturerId: {2}";
        public const string CourseCreated = "Course created successfully with ID: {0}";
        public const string CourseUpdating = "Updating course with ID: {0}";
        public const string CourseDeleting = "Deleting course with ID: {0}";
        public const string CourseCodeDuplicateError = "Course creation failed due to duplicate course code: {0}";

        // Access code logging
        public const string AccessCodeGenerating = "Generating access code for course {0}";
        public const string AccessCodeValidating = "Validating access code for course {0}";
        public const string AccessCodeValidated = "Access code validated successfully for course {0}";
        public const string AccessCodeInvalid = "Access code validation failed for course {0}: Invalid code";
        public const string AccessCodeExpiredLog = "Access code validation failed for course {0}: Code expired";
        public const string AccessCodeAttemptFailed = "Failed access code attempt #{0} for course {1}";
        public const string AccessCodeUpdated = "Access code updated for course {0} by lecturer {1}";

        // Enrollment logging
        public const string StudentEnrolling = "Enrolling student {0} in course {1}";
        public const string StudentEnrolled = "Student {0} successfully enrolled in course {1}";
        public const string StudentUnenrolling = "Unenrolling student {0} from course {1}";
        public const string StudentUnenrolled = "Student {0} successfully unenrolled from course {1}";
        public const string StudentJoiningCourse = "Student {0} attempting to join course {1}";
        public const string StudentJoinedCourse = "Student {0} successfully joined course {1}";

        // Group logging
        public const string GroupCreating = "Creating group '{0}' in course {1}";
        public const string GroupCreated = "Group created successfully with ID: {0}";
        public const string GroupUpdating = "Updating group with ID: {0}";
        public const string GroupDeleting = "Deleting group with ID: {0}";
        public const string RandomizingGroups = "Randomizing {0} students into {1} groups for course {2}";
        public const string GroupsRandomized = "Successfully created {0} groups with randomized students";
        public const string AssigningGroupToAssignment = "Assigning group {0} to assignment {1}";

        // GroupMember logging
        public const string MemberAdding = "Adding student {0} to group {1}";
        public const string MemberAdded = "Student {0} successfully added to group {1}";
        public const string AddingMultipleMembers = "Adding {0} students to group {1}";
        public const string RandomizingStudents = "Randomizing {0} students into groups of size {1} for course {2}";
        public const string GroupsCreated = "Created {0} groups with {1} students for course {2}";
        public const string AssigningLeader = "Assigning student {0} as leader of group {1}";
        public const string MemberRemoving = "Removing student {0} from group {1}";
        public const string MemberRemoved = "Student {0} successfully removed from group {1}";
        public const string LeaderSetting = "Setting student {0} as leader of group {1}";
        public const string LeaderSet = "Student {0} set as leader of group {1}";
        public const string StudentJoiningGroup = "Student {0} attempting to join group {1}";
        public const string StudentJoinedGroup = "Student {0} successfully joined group {1}";
        public const string StudentLeavingGroup = "Student {0} leaving group {1}";
        public const string StudentLeftGroup = "Student {0} successfully left group {1}";

        // User validation logging
        public const string ValidatingLecturer = "Validating lecturer with ID: {0}";
        public const string LecturerFound = "Found lecturer: {0} {1} (Role: {2})";
        public const string LecturerNotFound = "Lecturer with ID {0} not found in UserService";
        public const string InvalidRole = "User {0} has role {1}, expected {2}";

        // Assignment logging
        public const string AssignmentCreating = "Creating assignment '{0}' for course {1}";
        public const string AssignmentCreatedLog = "Assignment created successfully with ID: {0}";
        public const string AssignmentUpdating = "Updating assignment with ID: {0}";
        public const string AssignmentDeleting = "Deleting assignment with ID: {0}";
        public const string AssignmentDeleted = "Assignment {0} deleted successfully";
        public const string AssignmentClosing = "Closing assignment with ID: {0}";
        public const string AssignmentClosedLog = "Assignment {0} closed successfully";
        public const string AssignmentDueDateExtending = "Extending due date for assignment {0} to {1}";
        public const string AssignmentDueDateExtended = "Assignment {0} due date extended to {1}";
        public const string AssigningGroupsToAssignment = "Assigning {0} groups to assignment {1}";
        public const string AssignedGroupsToAssignment = "Assigned {0} groups to assignment {1}";
        public const string UnassigningGroupsFromAssignment = "Unassigning {0} groups from assignment {1}";
        public const string UnassignedGroupsFromAssignment = "Unassigned {0} groups from assignment {1}";
    }

    /// <summary>
    /// Response messages with pagination info
    /// </summary>
    public static class Pagination
    {
        public const string CoursesRetrieved = "Successfully retrieved {0} courses (page {1} of {2})";
        public const string EnrollmentsRetrieved = "Successfully retrieved {0} enrollments (page {1} of {2})";
        public const string AvailableCoursesRetrieved = "Successfully retrieved {0} available courses (page {1} of {2})";
        public const string GroupsRetrieved = "Successfully retrieved {0} groups (page {1} of {2})";
        public const string MembersRetrieved = "Successfully retrieved {0} members (page {1} of {2})";
    }

    /// <summary>
    /// Domain event messages
    /// </summary>
    public static class Events
    {
        public const string CourseCreatedEvent = "Course created event published for course {0}";
        public const string StudentEnrolledEvent = "Student enrolled event published for enrollment {0}";
        public const string StudentUnenrolledEvent = "Student unenrolled event published for enrollment {0}";
        public const string AccessCodeUpdatedEvent = "Access code updated event published for course {0}";
        public const string GroupCreatedEvent = "Group created event published for group {0}";
        public const string GroupMemberAddedEvent = "Group member added event published for member {0}";
        public const string GroupMemberRemovedEvent = "Group member removed event published for member {0}";
        public const string GroupLeaderChangedEvent = "Group leader changed event published for group {0}";
        public const string GroupAssignmentAssignedEvent = "Group assignment assigned event published for group {0}";
    }

    /// <summary>
    /// Helper methods for formatting messages
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Formats an error message with parameters
        /// </summary>
        public static string FormatError(string template, params object[] args)
        {
            return string.Format(template, args);
        }

        /// <summary>
        /// Formats a success message with parameters
        /// </summary>
        public static string FormatSuccess(string template, params object[] args)
        {
            return string.Format(template, args);
        }

        /// <summary>
        /// Formats the course code exists message
        /// </summary>
        public static string FormatCourseCodeExists(string courseCode)
        {
            return string.Format(Error.CourseCodeExists, courseCode);
        }

        /// <summary>
        /// Formats the course exists message
        /// </summary>
        public static string FormatCourseExists(string courseCode)
        {
            return string.Format(Error.CourseExists, courseCode);
        }
    }
}