using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Common;
using MOE_System.Application.DTOs.Course.Request;
using MOE_System.Application.DTOs.Course.Response;
using MOE_System.Application.Interfaces;

namespace MOE_System.API.Controllers
{
    [Route("api/v1/admin/courses")]
    public class CourseController : BaseApiController
    {
        private readonly ICourseService _courseService;

        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PaginatedList<CourseListResponse>>>> GetCourses([FromQuery] GetCourseRequest request, CancellationToken cancellationToken)
        {
            var courses = await _courseService.GetCoursesAsync(request, cancellationToken);
            return Success(courses, "Courses retrieved successfully");
        }

        [HttpGet("{courseId}")]
        public async Task<ActionResult<ApiResponse<CourseDetailResponse?>>> GetCourseDetail(string courseId, CancellationToken cancellationToken)
        {
            var courseDetail = await _courseService.GetCourseDetailAsync(courseId, cancellationToken);
            return Success(courseDetail, "Course detail retrieved successfully");
        }

        [HttpPut("{courseId}")]
        public async Task<ActionResult<ApiResponse>> UpdateCourse(string courseId, [FromBody] UpdateCourseRequest request, CancellationToken cancellationToken)
        {
            await _courseService.UpdateCourseAsync(courseId, request, cancellationToken);
            return Success("Course updated successfully");
        }

        [HttpDelete("{courseId}")]
        public async Task<ActionResult<ApiResponse>> DeleteCourse(string courseId, CancellationToken cancellationToken)
        {
            await _courseService.DeleteCourseAsync(courseId, cancellationToken);
            return Success("Course deleted successfully");
        }

        [HttpGet("{courseId}/non-enrolled-accounts")]
        public async Task<ActionResult<ApiResponse<NonEnrolledAccountResponse>>> GetNonEnrolledAccounts(string courseId)
        {
            var nonEnrolledAccounts = await _courseService.GetNonEnrolledAccountAsync(courseId);
            return Success(nonEnrolledAccounts, "Non-enrolled accounts retrieved successfully");
        }
        /// <summary>
        /// Add a new course to the system
        /// </summary>
        /// <param name="request">The course details to add</param>
        /// <returns>The created course information</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CourseResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponse<CourseResponse>>> AddCourse([FromBody] AddCourseRequest request)
        {
            var course = await _courseService.AddCourseAsync(request);
            return Created(course, "Course added successfully");
        }

        [HttpPost("{courseId}/bulk-enroll")]
        public async Task<ActionResult<ApiResponse>> BulkEnrollAccount(string courseId, [FromBody] BulkEnrollAccountAsync request)
        {
            request.CourseId = courseId;
            await _courseService.BulkEnrollAccountAsync(request);
            return Success("Accounts enrolled successfully");
        }

        [HttpDelete("{courseId}/bulk-remove")]
        public async Task<ActionResult<ApiResponse>> BulkRemoveEnrolledAccount(string courseId, [FromBody] BulkRemoveEnrolledAccountRequest request)
        {
            request.CourseId = courseId;
            await _courseService.BulkRemoveEnrolledAccountAsync(request);
            return Success("Accounts removed successfully");
        }
    }
}
