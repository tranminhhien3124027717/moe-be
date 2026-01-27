using MediatR;
using Microsoft.EntityFrameworkCore;
using MOE_System.Application.Common.Events;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Application.Common.Handlers
{
    public class CheckUserEducationLevelHandler : INotificationHandler<CheckUserEducationLevelEvent>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CheckUserEducationLevelHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork; 
        }

        public async Task Handle(CheckUserEducationLevelEvent notification, CancellationToken cancellationToken)
        {
            var accountRepo = _unitOfWork.GetRepository<AccountHolder>();
            var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();

            // 1. CHECK USER
            var user = await accountRepo.Entities
                .FirstOrDefaultAsync(u => u.Id == notification.AccountHolderId, cancellationToken);

            if (user == null) return;

            // 2. LẤY MAX LEVEL TỪ ENROLLMENT
            // Lấy list các level từ các khóa học (Active, Inactive, Completed...)
            var enrollmentLevels = await enrollmentRepo.Entities
                .AsNoTracking()
                .Where(e => e.EducationAccount.AccountHolderId == notification.AccountHolderId
                            && e.Course != null)
                .Select(e => (int)e.Course.EducationLevel) 
                .ToListAsync(cancellationToken);

            // Tìm level cao nhất trong các khóa học (Nếu ko học gì thì max = 0)
            int maxEnrollmentLevel = enrollmentLevels.Any() ? enrollmentLevels.Max() : 0;

            // 3. SO SÁNH VỚI LEVEL HIỆN TẠI CỦA USER
            // Logic: "Đã đạt được thì là mãi mãi" -> Chỉ cập nhật nếu tìm thấy cái cao hơn
            // Lấy level hiện tại của user (ép về int để so sánh)
            int currentUserLevel = (int)user.EducationLevel;

            // Nếu Max mới tìm được > Level hiện tại của user -> Cập nhật lên
            if (maxEnrollmentLevel != currentUserLevel)
            {
                user.EducationLevel = (EducationLevel)maxEnrollmentLevel;

                await accountRepo.UpdateAsync(user);

                await _unitOfWork.SaveAsync();
            }
        }
    
    }
}
