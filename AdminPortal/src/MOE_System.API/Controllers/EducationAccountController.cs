using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Common;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers;
[Route("api/v1/admin/education-accounts")]
public class EducationAccountController : BaseApiController
{
    private readonly IEducationAccountService _educationAccountService;

    public EducationAccountController(IEducationAccountService educationAccountService)
    {
        _educationAccountService = educationAccountService;
    }

    [HttpDelete("{educationAccountId}/close")]
    public async Task<ActionResult<ApiResponse>> CloseEducationAccounts([FromRoute] string educationAccountId, CancellationToken cancellationToken)
    {
        await _educationAccountService.CloseEducationAccountManuallyAsync(educationAccountId, cancellationToken);
        return Success("Education account closed successfully");
    }
}