using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MOE_System.Application.Services
{
    public class TopUpConfigService : ITopUpConfigService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TopUpConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task CreateTopUpConfigAsync(TopUpConfig topUpConfig)
        {
            var newConfig = new TopUpConfig
            {
                Id = Guid.NewGuid().ToString(),
                RuleName = topUpConfig.RuleName,
                TopupAmount = topUpConfig.TopupAmount,
                MinAge = topUpConfig.MinAge,
                MaxAge = topUpConfig.MaxAge,
                MinBalance = topUpConfig.MinBalance,
                MaxBalance = topUpConfig.MaxBalance,
                EducationLevels = topUpConfig.EducationLevels,
                SchoolingStatuses = topUpConfig.SchoolingStatuses,
                InternalRemarks = topUpConfig.InternalRemarks,
                CreatedAt = DateTime.UtcNow
            };
            
            var repo = _unitOfWork.GetRepository<TopUpConfig>();  
            await repo.InsertAsync(newConfig);

            await repo.SaveAsync();
        }

        public async Task<bool> DeleteTopUpConfigAsync(string id)
        {
            var repo = _unitOfWork.GetRepository<TopUpConfig>();  
            var existingConfig = await repo.GetByIdAsync(id);

            if (existingConfig == null)
            {
                return false; // Not found
            }

            await repo.DeleteAsync(existingConfig);
            await repo.SaveAsync();

            return true;
        }

        public async Task<List<TopUpConfig>> GetTopUpConfigAsync(string? searchTerm)
        {
            var repo = _unitOfWork.GetRepository<TopUpConfig>();  
            var query = repo.Entities.AsQueryable();
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.RuleName.Contains(searchTerm));
            }

            var result = await query
                .OrderByDescending(c => c.RuleName)
                .ToListAsync();

            return result;
        }
    }
}
