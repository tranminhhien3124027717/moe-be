using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.Infrastructure.Jobs
{
    public class EducationLevelAutoTrigger : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public EducationLevelAutoTrigger(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {// Delay 30s khi khởi động server
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanAndFireEducationLevelEventsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EducationLevelTrigger] Error: {ex.Message}");
                }

                // Education Level ít thay đổi hơn Status đi học, nên có thể quét thưa hơn
                // Ví dụ: 30 phút hoặc 1 tiếng. Ở đây để 30 phút.
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ScanAndFireEducationLevelEventsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var educationService = scope.ServiceProvider.GetRequiredService<IStudentStatusService>(); // Check lại tên Interface
            var accountRepo = uow.GetRepository<AccountHolder>();

            // 1. LẤY DỮ LIỆU THÔ VỀ RAM (Safe Mode)
            // Chỉ lấy ID, Level hiện tại và List level đang học.
            // Không tính toán Max ở đây để tránh lỗi SQL.
            var rawData = await accountRepo.Entities
                .AsNoTracking()
                .Where(u => u.EducationAccount.Enrollments.Any() || u.EducationLevel != EducationLevel.NotSet) // Chỉ lấy người có đi học hoặc có level khác None
                .Select(u => new
                {
                    UserId = u.Id,
                    CurrentLevelInt = (int)u.EducationLevel,
                    // Lấy danh sách level về RAM
                    EnrolledLevels = u.EducationAccount.Enrollments
                                        .Where(e => e.Course != null)
                                        .Select(e => (int)e.Course.EducationLevel) // Check lại tên property trong Course
                                        .ToList()
                })
                .ToListAsync();

            if (!rawData.Any()) return;

            var listUserIdsToFix = new List<string>();

            // 2. TÍNH TOÁN BẰNG C# (Chính xác tuyệt đối)
            foreach (var user in rawData)
            {
                // Tính Max thực tế (nếu list rỗng thì là 0)
                int realMaxLevel = user.EnrolledLevels.Any() ? user.EnrolledLevels.Max() : 0;

                // So sánh: Nếu thực tế KHÁC trong DB thì thêm vào list sửa
                if (realMaxLevel != user.CurrentLevelInt)
                {
                    listUserIdsToFix.Add(user.UserId);
                }
            }

            if (!listUserIdsToFix.Any()) return;

            Console.WriteLine($"[EducationTrigger] Tìm thấy {listUserIdsToFix.Count} user sai Level. Đang đẩy vào Outbox...");

            // 3. BẮN EVENT
            foreach (var userId in listUserIdsToFix)
            {
                await educationService.TriggerEducationLevelCheckAsync(userId.ToString());
            }
        }
    }
}
