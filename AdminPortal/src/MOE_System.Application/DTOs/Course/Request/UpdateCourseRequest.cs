namespace MOE_System.Application.DTOs.Course.Request;

public sealed record UpdateCourseRequest
(
    string CourseName,
    string? EducationLevel,
    string LearningType,
    string? Status
);