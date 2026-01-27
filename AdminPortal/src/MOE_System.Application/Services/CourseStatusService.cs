using Microsoft.EntityFrameworkCore;
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
    public class CourseStatusService : ICourseStatusService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseStatusService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task TriggerCourseStatusCheckAsync(CancellationToken cancellationToken)
        {
            var courseRepo = _unitOfWork.GetRepository<Course>();
            var currentDate = DateTime.UtcNow; // Dùng UTC cho chuẩn

            // -- BƯỚC A: LẤY DỮ LIỆU --
            // Lấy tất cả các khóa đang Active nhưng ngày kết thúc < hiện tại
            var expiredCourses = await courseRepo.Entities
                .Where(c => c.Status == "Active" && c.EndDate < currentDate)
                .ToListAsync(cancellationToken);

            if (!expiredCourses.Any()) return; // Không có gì thì thôi, nghỉ

            // -- BƯỚC B: XỬ LÝ (UPDATE) --
            foreach (var course in expiredCourses)
            {
                // 1. Đổi trạng thái
                course.Status = "Inactive";

            }

            // -- BƯỚC C: LƯU DB (QUAN TRỌNG) --
            // Save 1 lần duy nhất cho toàn bộ danh sách (Nhanh hơn cách cũ 100 lần)
            await _unitOfWork.SaveAsync();

            //Console.WriteLine($"[CourseService] Đã đóng {expiredCourses.Count} khóa học hết hạn.");
        }
    }
}
