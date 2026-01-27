using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Common;
using MOE_System.Application.DTOs.Auth;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers
{
    [ApiController]
    [Route("api/v1/admin/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request.Username, request.Password);

           return result
                ? ApiResponse.SuccessResponse("Login successful")
                : ApiResponse.ErrorResponse("Invalid username or password");
        }
    }
}
