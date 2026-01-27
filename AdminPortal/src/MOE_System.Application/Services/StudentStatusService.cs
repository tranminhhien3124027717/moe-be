using MOE_System.Application.Common.Events;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MOE_System.Application.Services
{
    public class StudentStatusService : IStudentStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StudentStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task TriggerEducationLevelCheckAsync(string accountHolderId)
        {
            var domainEvent = new CheckUserEducationLevelEvent
            {
                AccountHolderId = accountHolderId
            };

            var outboxMessage = new OutBoxMessage
            {
                Id = Guid.NewGuid().ToString(),
                OccurredOn = DateTime.UtcNow,
                Type = typeof(CheckUserEducationLevelEvent).AssemblyQualifiedName,
                Content = JsonSerializer.Serialize(domainEvent),
                ProcessedOn = null
            };

            var repo = _unitOfWork.GetRepository<OutBoxMessage>();

            await repo.InsertAsync(outboxMessage);
            await _unitOfWork.SaveAsync();
        }

        public async Task TriggerSchoolingStatusCheckAsync(string accountHolderId)
        {
            // 1. Tạo Event
            var domainEvent = new CheckUserStatusEvent { AccountHolderId = accountHolderId };

            // 2. Tạo bản ghi Outbox
            var outboxMessage = new OutBoxMessage
            {
                Id = Guid.NewGuid().ToString(),
                OccurredOn = DateTime.UtcNow,
                Type = typeof(CheckUserStatusEvent).AssemblyQualifiedName, 
                Content = JsonSerializer.Serialize(domainEvent),
                ProcessedOn = null // Quan trọng: Null = Chưa xử lý
            };

            var repo = _unitOfWork.GetRepository<OutBoxMessage>();

            // 3. Lưu vào DB (Chưa bắn Event vội)
            await repo.InsertAsync(outboxMessage);
            await _unitOfWork.SaveAsync();
        }
    }
}
