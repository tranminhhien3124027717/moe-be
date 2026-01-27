using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MOE_System.EService.Application.Common;
using MOE_System.EService.Application.DTOs.Course;
using MOE_System.EService.Application.Interfaces.Services;
using System.Security.Claims;

namespace MOE_System.EService.API.Controllers;

[Route("api/v1/your-courses")]
[Authorize]
public class CourseController : BaseApiController
{
    private readonly IAccountHolderService _accountHolderService;

    public CourseController(IAccountHolderService accountHolderService)
    {
        _accountHolderService = accountHolderService;
    }

    /// <summary>
    /// Get detailed information about a specific enrolled course
    /// </summary>
    /// <param name="enrollmentId">The enrollment ID</param>
    /// <returns>Course detail with payment history</returns>
    [HttpGet("{enrollmentId}")]
    public async Task<ActionResult<ApiResponse<CourseDetailResponse>>> GetCourseDetail([FromRoute] string enrollmentId)
    {
        var accountHolderId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(accountHolderId))
        {
            return Unauthorized(ApiResponse<CourseDetailResponse>.ErrorResponse("User not authenticated", 401));
        }

        var courseDetail = await _accountHolderService.GetCourseDetailAsync(accountHolderId, enrollmentId);
        
        return Success(courseDetail, "Course details retrieved successfully");
    }
}
