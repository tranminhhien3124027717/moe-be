using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Interfaces;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Text;

namespace MOE_System.Infrastructure.Jobs
{
    public class CourseStatusAutoTrigger : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public CourseStatusAutoTrigger(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Delay 1 chút khi khởi động server
            await Task.Delay(TimeSpan.FromSeconds(40), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Tạo scope mới để lấy Service (Bắt buộc với BackgroundService)
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var courseService = scope.ServiceProvider.GetRequiredService<ICourseStatusService>();

                        // GỌI HÀM BẠN VỪA VIẾT Ở BƯỚC 1
                        await courseService.TriggerCourseStatusCheckAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CourseTrigger] Lỗi: {ex.Message}");
                }

                // Logic này không cần chạy liên tục. 1 Tiếng quét 1 lần là quá đủ.
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

    }
}
