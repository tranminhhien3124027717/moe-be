using MOE_System.Application.Common;
using MOE_System.Application.DTOs.Course.Request;
using MOE_System.Application.DTOs.Course.Response;

namespace MOE_System.Application.Interfaces
{
    public interface ICourseService
    {
        Task<PaginatedList<CourseListResponse>> GetCoursesAsync(GetCourseRequest request, CancellationToken cancellationToken);
        Task<CourseDetailResponse?> GetCourseDetailAsync(string courseId, CancellationToken cancellationToken = default);
        Task<NonEnrolledAccountResponse> GetNonEnrolledAccountAsync(string courseId, CancellationToken cancellationToken = default);
        Task<CourseResponse> AddCourseAsync(AddCourseRequest request);
        Task UpdateCourseAsync(string courseId, UpdateCourseRequest request, CancellationToken cancellationToken = default);
        Task DeleteCourseAsync(string courseId, CancellationToken cancellationToken = default);
        Task BulkEnrollAccountAsync(BulkEnrollAccountAsync request);
        Task BulkRemoveEnrolledAccountAsync(BulkRemoveEnrolledAccountRequest request);
    }
}
