using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MOE_System.Application.Common.Course;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.DTOs.Invoice.Request;
using MOE_System.Application.DTOs.Invoice.Response;
using MOE_System.Application.Interfaces.Services;
using MOE_System.Domain.Common;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;


namespace MOE_System.Application.Services;

public sealed class InvoiceService : IInvoiceService
{
    private const int BillingWindowDays = 2;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateInvoiceForEnrollmentAsync(DateTime logicalDate, CancellationToken cancellationToken = default)
    {
        var runDate = logicalDate.Date;

        var courseRepository = _unitOfWork.GetRepository<Course>();

        var courses = await courseRepository.ToListAsync(
            predicate: c =>
                c.Status == "Active" &&
                (c.PaymentType == PaymentType.Recurring || c.PaymentType == PaymentType.OneTime) &&
                c.BillingDate.HasValue &&
                c.BillingCycle != null &&
                runDate < c.EndDate.AddMonths(1),
            cancellationToken: cancellationToken
        );

        foreach (var course in courses)
        {
            await ProcessCourseAsync(course, runDate, cancellationToken);
        }
    }

    private async Task ProcessCourseAsync(Course course, DateTime runDate, CancellationToken cancellationToken)
    {
        DateTime periodStart, periodEnd;

        if (course.PaymentType == PaymentType.OneTime)
        {
            periodStart = course.StartDate;
            periodEnd = course.EndDate;
        }
        else
        {
            var periods = BillingPeriodResolver.Resolve(
                course.StartDate,
                course.BillingCycle!,
                course.EndDate
            );

            var current = periods.FirstOrDefault(p =>
            {
                var periodBillingDate = ResolveBillingDate(course, p.Start);

                return IsWithinBillingWindow(runDate, periodBillingDate);
            });

            if (current == default)
            {
                current = periods.FirstOrDefault(p => p.Start <= runDate && runDate < p.End);
            }

            if (current == default)
            {
                current = periods.FirstOrDefault(p =>
                {
                    var billingDate = ResolveBillingDate(course, p.Start);
                    return billingDate < runDate && runDate < p.End;
                });
            }

            if (current == default)
            {
                return;
            }

            periodStart = current.Start;
            periodEnd = current.End;
        }

        if (periodStart >= course.EndDate)
        {
            return;
        }

        var enrollmentRepository = _unitOfWork.GetRepository<Enrollment>();
        var invoiceRepository = _unitOfWork.GetRepository<Invoice>();

        var enrollments = await enrollmentRepository.ToListAsync(
            predicate: e =>
                e.CourseId == course.Id &&
                e.EnrollDate < periodEnd,
            cancellationToken: cancellationToken
        );

        if (enrollments.Count == 0) return;

        var existingInvoices = await invoiceRepository.ToListAsync(
            predicate: i =>
                i.Enrollment!.CourseId == course.Id &&
                (course.PaymentType == PaymentType.OneTime || i.BillingPeriodStart == periodStart),
            cancellationToken: cancellationToken
        );

        var invoiceLookup = existingInvoices
            .ToLookup(i => i.EnrollmentID);

        var billingDate = ResolveBillingDate(
            course,
            course.PaymentType == PaymentType.OneTime ? course.StartDate : periodStart
        );

        var isWithinWindow = IsWithinBillingWindow(runDate, billingDate);

        if (!isWithinWindow && course.PaymentType == PaymentType.OneTime && runDate >= billingDate)
        {
            isWithinWindow = true;
        }

        foreach (var enrollment in enrollments)
        {
            if (invoiceLookup.Contains(enrollment.Id))
                continue;

            if (!isWithinWindow && enrollment.EnrollDate <= billingDate)
                continue;

            var invoice = BuildInvoice(
                enrollment,
                course,
                periodStart,
                periodEnd,
                billingDate
            );

            await invoiceRepository.InsertAsync(invoice);
        }

        try
        {
            await _unitOfWork.SaveAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Log and ignore duplicate invoice creation attempts
        }
    }

    private static Invoice BuildInvoice(
        Enrollment enrollment,
        Course course,
        DateTime periodStart,
        DateTime periodEnd,
        DateTime billingDate)
    {
        var status = enrollment.EnrollDate <= billingDate
            ? InvoiceStatus.Scheduled
            : InvoiceStatus.Outstanding;

        return new Invoice
        {
            EnrollmentID = enrollment.Id,
            Amount = course.FeePerCycle!.Value,
            Status = status,

            PaymentType = course.PaymentType,
            BillingCycle = course.BillingCycle,
            BillingDate = billingDate,

            BillingPeriodStart = periodStart,
            BillingPeriodEnd = periodEnd,

            PaymentDue = course.PaymentDue,
            DueDate = course.PaymentDue.HasValue
                ? billingDate.AddDays(course.PaymentDue.Value)
                : null
        };
    }

    private static DateTime ResolveBillingDate(Course course, DateTime referenceDate)
    {
        if (!course.BillingDate.HasValue)
        {
            return referenceDate;
        }

        var billingDay = course.BillingDate.Value;
        var day = Math.Min(
            billingDay,
            DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month)
        );

        return new DateTime(referenceDate.Year, referenceDate.Month, day);
    }

    private static bool IsWithinBillingWindow(
        DateTime runDate,
        DateTime billingDate)
    {
        return runDate >= billingDate && runDate <= billingDate.AddDays(BillingWindowDays);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number == 2601 || sqlEx.Number == 2627;
        }

        return false;
    }

    public async Task<PrintInvoiceResponse> GetInvoiceAsync(PrintInvoiceRequest request)
    {
        // Get the latest invoice for this enrollment and course
        var enrollmentRepo = _unitOfWork.GetRepository<Enrollment>();
        var invoiceRepo = _unitOfWork.GetRepository<Invoice>();

        var enrollment = await enrollmentRepo.FirstOrDefaultAsync(
            predicate: e => e.EducationAccountId == request.EducationAccountId && e.CourseId == request.CourseId
        );

        if (enrollment == null)
        {
            throw new BaseException.NotFoundException("Enrollment is not found!");
        }

        // Get the latest invoice or create if not exists
        var invoice = enrollment.Invoices?.OrderByDescending(i => i.Id).FirstOrDefault();

        if (invoice == null)
        {
            // Create invoice using the same logic as batch generation
            var course = enrollment.Course;
            if (course == null)
                throw new BaseException.NotFoundException("Course is not found!");

            var now = DateTime.UtcNow;
            var periods = BillingPeriodResolver.Resolve(
                course.StartDate,
                course.BillingCycle!,
                course.EndDate
            );

            var period = periods.FirstOrDefault(p => p.Start <= now && now < p.End);
            if (period == default)
            {
                throw new BaseException.NotFoundException("No billing period found for invoice generation.");
            }

            var billingDate = ResolveBillingDate(course, period.Start);

            invoice = BuildInvoice(
                enrollment,
                course,
                period.Start,
                period.End,
                billingDate
            );

            await invoiceRepo.InsertAsync(invoice);
            await _unitOfWork.SaveAsync();
        }

        return new PrintInvoiceResponse
        {
            InvoiceId = invoice.Id,
            EnrollmentId = invoice.EnrollmentID,
            Status = invoice.Status == InvoiceStatus.Paid ? PaymentStatus.Paid : PaymentStatus.Outstanding,
            DueDate = invoice.DueDate ?? DateTime.MinValue,
            Amount = invoice.Amount,
        };
    }
}

