using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces; // Để gọi Trigger
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;

namespace MOE_System.Infrastructure.BackgroundJobs
{
    public class SchoolingStatusAutoTrigger : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SchoolingStatusAutoTrigger(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Chạy quét dọn
                    await ScanAndFireEventsAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"AutoTrigger Error: {ex.Message}");
                }

                // Cứ 10 phút quét 1 lần (hoặc 1 tiếng tùy bạn)
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task ScanAndFireEventsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Đây chính là mấu chốt: Inject cái Service ghi Outbox
            var statusService = scope.ServiceProvider.GetRequiredService<IStudentStatusService>();

            var accountRepo = uow.GetRepository<AccountHolder>();

            // 1. TÌM NGƯỜI CÓ DẤU HIỆU SAI (Đang NotInSchool mà lại có khóa học Active)
            // Lưu ý: Chỉ lấy ID để nhẹ DB
            var usersShouldBeInSchool = await accountRepo.Entities
                .Where(u => u.SchoolingStatus == SchoolingStatus.NotInSchool
                            && u.EducationAccount.Enrollments.Any())
                .Select(u => u.Id)
                .ToListAsync();

            // 2. TÌM NGƯỜI CÓ DẤU HIỆU SAI (Đang InSchool mà KHÔNG CÒN khóa học Active)
            var usersShouldBeNotInSchool = await accountRepo.Entities
                .Where(u => u.SchoolingStatus == SchoolingStatus.InSchool
                            && !u.EducationAccount.Enrollments.Any())
                .Select(u => u.Id)
                .ToListAsync();

            // Gộp danh sách lại
            var listIds = usersShouldBeInSchool.Concat(usersShouldBeNotInSchool).Distinct().ToList();

            if (!listIds.Any()) return;

            Console.WriteLine($"[AutoTrigger] Phát hiện {listIds.Count} user cần cập nhật lại status. Đang đẩy vào Outbox...");

            // 3. BẮN VÀO OUTBOX (Thay vì update trực tiếp)
            // Đoạn này sẽ insert vào bảng OutboxMessages, sau đó OutboxWorker sẽ lo phần còn lại
            foreach (var userId in listIds)
            {
                await statusService.TriggerSchoolingStatusCheckAsync(userId);
            }
        }
    }
}