using Microsoft.AspNetCore.Mvc;
using MOE_System.Application.Interfaces.Services;

namespace MOE_System.API.Controllers
{
    [Route("api/v1/admin/topup-configs")]
    public class TopUpConfigController : BaseApiController
    {
        private readonly ITopUpConfigService _topUpConfigService;
        public TopUpConfigController(ITopUpConfigService topUpConfigService)
        {
            _topUpConfigService = topUpConfigService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTopUpConfigs(string? searchTerm)
        {
            var configs = await _topUpConfigService.GetTopUpConfigAsync(searchTerm);
            return Ok(configs);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTopUpConfig([FromBody] Domain.Entities.TopUpConfig topUpConfig)
        {
            await _topUpConfigService.CreateTopUpConfigAsync(topUpConfig);
            return Ok();
        }   

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopUpConfig(string id)
        {
            var result = await _topUpConfigService.DeleteTopUpConfigAsync(id);
            if (!result)
            {
                return NotFound();
            }
            return Ok();
        }
    }
}
