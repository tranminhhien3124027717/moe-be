using Microsoft.EntityFrameworkCore;
using MOE_System.EService.Application.Common.Interfaces;
using MOE_System.EService.Application.DTOs.AccountHolder;
using MOE_System.EService.Application.DTOs.Dashboard;
using MOE_System.EService.Application.Interfaces.Services;
using MOE_System.EService.Domain.Entities;
using MOE_System.EService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.EService.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DashboardResponse> GetAccountDashboardAsync(string accountHolderId)
        {
            var accountHolderRepo = _unitOfWork.GetRepository<AccountHolder>();

            var accountHolder = await accountHolderRepo.Entities
                .Where(ah => ah.Id == accountHolderId)
                .Include(ah => ah.EducationAccount)
                .Include(ah => ah.EducationAccount)
                    .ThenInclude(ea => ea!.Enrollments)
                        .ThenInclude(e => e.Invoices)
                .FirstOrDefaultAsync();

            if (accountHolder == null)
            {
                throw new KeyNotFoundException("Account holder not found.");
            }

            var eduAccount = accountHolder.EducationAccount;

            if (eduAccount == null)
            {
                return new DashboardResponse();
            }

            var enrollments = eduAccount.Enrollments ?? new List<Enrollment>();

            var activeCoursesCount = enrollments.Count();
            //var activeCoursesCount = enrollments.Count(e => e.Status == PaymentStatus.Scheduled);

            var outstandingFees = enrollments
                .Sum(e => e.Invoices
                    .Where(i => i.Status == InvoiceStatus.Outstanding)
                    .Sum(i => i.Amount));

            var outstadingCount = enrollments
                .Count(e => e.Invoices
                    .Any(i => i.Status == InvoiceStatus.Outstanding));

            var enrollRepo = _unitOfWork.GetRepository<Enrollment>();

            var rawData = await enrollRepo.Entities
                .Where(e => e.EducationAccountId == eduAccount.Id)
                .OrderByDescending(e => e.EnrollDate)
                .Take(5)
                .Include(e => e.Course)
                .Include(e => e.Invoices)
                .ToListAsync();

            var enrollCourses = rawData.Select(e => new EnrollCourse
            {
                EnrollmentId = e.Id,
                CourseName = e.Course?.CourseName ?? string.Empty,
                ProviderName = e.Course?.Provider?.Name ?? string.Empty,
                PaymentType = e.Course?.PaymentType.ToString() ?? string.Empty,
                BillingCycle = e.Course?.BillingCycle ?? string.Empty,
                EnrollDate = e.EnrollDate.ToString("dd/MM/yyyy"),
                BillingDate = GetBillingDateForEnrollment(e),
                PaymentStatus = GetPaymentStatusForDashboard(e)
            }).AsQueryable();

            return new DashboardResponse
            {
                FullName = accountHolder.FullName,
                Balance = eduAccount.Balance, 
                ActiveCoursesCount = activeCoursesCount,
                OutstandingFees = outstandingFees,
                OutstadingCount = outstadingCount,
                EnrollCourses = enrollCourses.ToList()
            };
        }

        private string CalculateNextBillingForDashboard(DateTime lastBillingDate, int billingDayOfMonth, string? billingCycle)
        {
            if (string.IsNullOrEmpty(billingCycle))
            {
                return "-";
            }

            int monthsToAdd = 0;

            switch (billingCycle.ToLower())
            {
                case "monthly":
                    monthsToAdd = 1;
                    break;
                case "quarterly":
                    monthsToAdd = 3;
                    break;
                case "biannually":
                    monthsToAdd = 6;
                    break;
                case "annually":
                case "yearly":
                    monthsToAdd = 12;
                    break;
                default:
                    return "-";
            }

            // Add the months to get to the next billing cycle
            var nextDate = lastBillingDate.AddMonths(monthsToAdd);

            // Set to the billing day of that month
            var daysInMonth = DateTime.DaysInMonth(nextDate.Year, nextDate.Month);
            var actualDay = Math.Min(billingDayOfMonth, daysInMonth);
            nextDate = new DateTime(nextDate.Year, nextDate.Month, actualDay);

            return nextDate.ToString("dd/MM/yyyy");
        }

        private string GetPaymentStatusForDashboard(Enrollment enrollment)
        {
            var totalFee = enrollment.Course?.FeeAmount ?? 0;
            var collectedFee = enrollment.Invoices?.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount) ?? 0;
            var hasPaidInvoices = enrollment.Invoices?.Any(i => i.Status == InvoiceStatus.Paid) ?? false;
            
            // Outstanding: has at least one outstanding invoice
            var hasOutstanding = enrollment.Invoices?.Any(i => i.Status == InvoiceStatus.Outstanding) ?? false;
            if (hasOutstanding)
            {
                return "Outstanding";
            }
            
            // Fully Paid: all invoices paid and collected >= total fee
            if (collectedFee >= totalFee)
            {
                return "Fully Paid";
            }
            
            // Paid: has at least one paid invoice (but not fully paid yet)
            if (hasPaidInvoices && collectedFee < totalFee)
            {
                return "Paid";
            }
            
            // Scheduled: no invoices OR no paid invoices yet
            return "Scheduled";
        }

        private string GetBillingDateForEnrollment(Enrollment enrollment)
        {
            var paymentStatus = enrollment.Status;
            var course = enrollment.Course;
            
            if (course == null || !course.BillingDate.HasValue)
            {
                return "-";
            }

            // Outstanding or Fully Paid: show "-"
            if (paymentStatus == PaymentStatus.FullyPaid)
            {
                return "-";
            }

            var currentDate = DateTime.UtcNow;
            
            // Check if we have any paid invoices
            var lastPaidInvoice = enrollment.Invoices?
                .Where(i => i.Status == InvoiceStatus.Paid)
                .OrderByDescending(i => i.BillingDate)
                .FirstOrDefault();

            if (lastPaidInvoice != null && !string.IsNullOrEmpty(course.BillingCycle))
            {
                // We have paid invoices - calculate next billing from last paid
                return CalculateNextBillingDateFromDate(lastPaidInvoice.BillingDate, course.BillingDate.Value, course.BillingCycle);
            }

            // No paid invoices yet - calculate first billing date
            // If course start date <= billing day in start month: bill on billing day
            // If course start date > billing day in start month: bill on start date
            //var courseStartDate = course.StartDate;
            var enrollmentDate = enrollment.EnrollDate;
            var billingDay = course.BillingDate.Value;
            
            if (enrollmentDate.Day <= billingDay)
            {
                // Bill on the billing day of the start month
                var daysInMonth = DateTime.DaysInMonth(enrollmentDate.Year, enrollmentDate.Month);
                var actualDay = Math.Min(billingDay, daysInMonth);
                var firstBillingDate = new DateTime(enrollmentDate.Year, enrollmentDate.Month, actualDay);
                return firstBillingDate.ToString("dd/MM/yyyy");
            }
            else
            {
                // Bill on course start date (since it's after billing day)
                return enrollmentDate.ToString("dd/MM/yyyy");
            }
        }

        private string CalculateNextBillingDateFromDate(DateTime lastBillingDate, int billingDayOfMonth, string? billingCycle)
        {
            if (string.IsNullOrEmpty(billingCycle))
            {
                return "-";
            }

            int monthsToAdd = 0;

            switch (billingCycle.ToLower())
            {
                case "monthly":
                    monthsToAdd = 1;
                    break;
                case "quarterly":
                    monthsToAdd = 3;
                    break;
                case "biannually":
                    monthsToAdd = 6;
                    break;
                case "annually":
                case "yearly":
                    monthsToAdd = 12;
                    break;
                default:
                    return "-";
            }

            // Add the months to get to the next billing cycle
            var nextDate = lastBillingDate.AddMonths(monthsToAdd);

            // Set to the billing day of that month
            var daysInMonth = DateTime.DaysInMonth(nextDate.Year, nextDate.Month);
            var actualDay = Math.Min(billingDayOfMonth, daysInMonth);
            nextDate = new DateTime(nextDate.Year, nextDate.Month, actualDay);

            return nextDate.ToString("dd/MM/yyyy");
        }
    }
}
