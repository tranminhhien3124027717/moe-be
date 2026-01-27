using MOE_System.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Interfaces.Services
{
    public interface ITopUpConfigService
    {
        Task<List<TopUpConfig>> GetTopUpConfigAsync(string? searchTerm);

        Task CreateTopUpConfigAsync(TopUpConfig topUpConfig);

        Task<bool> DeleteTopUpConfigAsync(string id);
    }
}
