using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Common;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers
{

    [ApiController]
    [Route("api/v1/student-statuses")]
    public class TestFlowController : ControllerBase
    {
        private readonly IStudentStatusService _studentStatusService;

        public TestFlowController(IStudentStatusService studentStatusService)
        {
            _studentStatusService = studentStatusService;
        }

        [HttpPost("trigger-check/{accountHolderId}")]
        public async Task<IActionResult> TriggerCheck([FromRoute] string accountHolderId)
        {
            await _studentStatusService.TriggerSchoolingStatusCheckAsync(accountHolderId);

            return Ok(new
            {
                Message = "Send successfully! Please wait for 2-5s",
                AccountHolderId = accountHolderId
            });
        }
    }
}
