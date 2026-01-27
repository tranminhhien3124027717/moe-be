using MediatR;
using Microsoft.EntityFrameworkCore;
using MOE_System.Application.Common.Events;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;

// Class Event bạn đã tạo ở bước trước
public class CheckUserStatusHandler : INotificationHandler<CheckUserStatusEvent>
{
    private readonly IUnitOfWork _unitOfWork;

    public CheckUserStatusHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(CheckUserStatusEvent notification, CancellationToken cancellationToken)
    {
        // 1. Lấy các Repo cần thiết
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();
        var accountRepo = _unitOfWork.GetRepository<AccountHolder>(); 

        // 2. LOGIC CHÍNH: Kiểm tra xem User có bất kỳ Enrollment nào đang Active không?
        bool isStudying = await enrollmentRepo.Entities
            .AnyAsync(e => e.EducationAccount.AccountHolderId == notification.AccountHolderId, cancellationToken); // Thêm điều kiện status enrollment nếu cần

        // 3. Lấy thông tin User hiện tại
        var user = await accountRepo.Entities
                        .FirstOrDefaultAsync(u => u.Id == notification.AccountHolderId, cancellationToken);

        if (user == null) return; // Không tìm thấy user thì bỏ qua

        // 4. Xác định Status mới
        var newStatus = isStudying ? SchoolingStatus.InSchool : SchoolingStatus.NotInSchool;

        // 5. Chỉ Update nếu Status thay đổi (Tối ưu DB)
        if (user.SchoolingStatus != newStatus)
        {
            user.SchoolingStatus = newStatus;

            await _unitOfWork.SaveAsync();
        }
    }
}