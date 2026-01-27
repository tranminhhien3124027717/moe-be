using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MOE_System.Application.Common.Interfaces;
using MOE_System.Application.Services;
using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;
using Xunit;

namespace MOE_System.Application.Tests.InvoiceServiceTests;

public class InvoiceServiceTests
{
    #region Setup and Mocks

    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Course>> _courseRepositoryMock;
    private readonly Mock<IGenericRepository<Enrollment>> _enrollmentRepositoryMock;
    private readonly Mock<IGenericRepository<Invoice>> _invoiceRepositoryMock;
    private readonly InvoiceService _invoiceService;

    public InvoiceServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _courseRepositoryMock = new Mock<IGenericRepository<Course>>();
        _enrollmentRepositoryMock = new Mock<IGenericRepository<Enrollment>>();
        _invoiceRepositoryMock = new Mock<IGenericRepository<Invoice>>();

        _unitOfWorkMock
            .Setup(u => u.GetRepository<Course>())
            .Returns(_courseRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(u => u.GetRepository<Enrollment>())
            .Returns(_enrollmentRepositoryMock.Object);

        _unitOfWorkMock
            .Setup(u => u.GetRepository<Invoice>())
            .Returns(_invoiceRepositoryMock.Object);

        _invoiceService = new InvoiceService(_unitOfWorkMock.Object);
    }

    private void SetupMocks(List<Course> courses, List<Enrollment> enrollments)
    {
        _courseRepositoryMock
            .Setup(r => r.ToListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>>(),
                It.IsAny<Func<IQueryable<Course>, IQueryable<Course>>>(),
                It.IsAny<Func<IQueryable<Course>, IOrderedQueryable<Course>>>(),
                It.IsAny<int>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                System.Linq.Expressions.Expression<Func<Course, bool>> predicate,
                Func<IQueryable<Course>, IQueryable<Course>> include,
                Func<IQueryable<Course>, IOrderedQueryable<Course>> orderBy,
                int limit,
                bool disableTracking,
                CancellationToken ct) =>
            {
                if (predicate == null)
                    return courses;
                
                var compiled = predicate.Compile();
                return courses.Where(compiled).ToList();
            });

        _enrollmentRepositoryMock
            .Setup(r => r.ToListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Enrollment, bool>>>(),
                It.IsAny<Func<IQueryable<Enrollment>, IQueryable<Enrollment>>>(),
                It.IsAny<Func<IQueryable<Enrollment>, IOrderedQueryable<Enrollment>>>(),
                It.IsAny<int>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                System.Linq.Expressions.Expression<Func<Enrollment, bool>> predicate,
                Func<IQueryable<Enrollment>, IQueryable<Enrollment>> include,
                Func<IQueryable<Enrollment>, IOrderedQueryable<Enrollment>> orderBy,
                int limit,
                bool disableTracking,
                CancellationToken ct) =>
            {
                if (predicate == null)
                    return enrollments;
                
                var compiled = predicate.Compile();
                return enrollments.Where(compiled).ToList();
            });

        // Mock invoice repository ToListAsync to return empty list (no existing invoices)
        _invoiceRepositoryMock
            .Setup(r => r.ToListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<int>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice>());
    }

    #endregion

    #region Helper Methods

    private Course CreateCourse(
        string id = "course-001",
        string billingCycle = "Monthly",
        int billingDate = 1,
        decimal feePerCycle = 100m,
        int? paymentDue = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        PaymentType paymentType = PaymentType.Recurring)
    {
        return new Course
        {
            Id = id,
            BillingCycle = billingCycle,
            BillingDate = billingDate,
            FeePerCycle = feePerCycle,
            PaymentDue = paymentDue,
            PaymentType = paymentType,
            Status = "Active",
            StartDate = startDate ?? new DateTime(2026, 1, 1),
            EndDate = endDate ?? new DateTime(2026, 12, 31)
        };
    }

    private Enrollment CreateEnrollment(
        string id = "enroll-001",
        string courseId = "course-001",
        DateTime? enrollDate = null)
    {
        return new Enrollment
        {
            Id = id,
            CourseId = courseId,
            Status = PaymentStatus.Scheduled,
            EnrollDate = enrollDate ?? new DateTime(2026, 1, 15),
            Invoices = new List<Invoice>()
        };
    }

    #endregion

    #region Scenario 1: StartDate > BillingDay (Charge After Course Starts)

    [Fact]
    public async Task Scenario1_FirstCycle_ShouldTriggerOnStartDate()
    {
        // Arrange: StartDate = 20, BillingDay = 15
        // First cycle: 20/01 -> 20/02
        // New requirement: billingDay (15) < courseStartDay (20) → billingDate = courseStart = 20/01
        // effectiveTriggerDate = 20/01 (StartDate)
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            feePerCycle: 100m,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 1, 20),
            "First cycle with billing day < course start day should use course start date");
        capturedInvoice.Status.Should().Be(InvoiceStatus.Scheduled,
            "Enrollment before trigger should be Scheduled");
        capturedInvoice.BillingPeriodStart.Should().Be(new DateTime(2026, 1, 20));
        capturedInvoice.BillingPeriodEnd.Should().Be(new DateTime(2026, 2, 20));
    }

    [Fact]
    public async Task Scenario1_FirstCycle_EnrollmentAfterTrigger_ShouldBeOutstanding()
    {
        // Arrange: Start 20/01, Trigger 20/01. Enrollment 21/01 (after trigger)
        // Cron runs on 21/01 (within 2-day window: 20, 21, 22)
        var logicalDate = new DateTime(2026, 1, 21); 
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 21));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.Status.Should().Be(InvoiceStatus.Outstanding);
    }

    [Fact]
    public async Task Scenario1_SecondCycle_ShouldTriggerOnBillingDay()
    {
        // Arrange: Cycle 1: 20/01 -> 20/02. Cycle 2: 20/02 -> 20/03.
        // BillingDay = 15. Cycle 2 trigger = 15/03 (Charge-before-learn)
        // Cron runs on 15/03
        var logicalDate = new DateTime(2026, 3, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 3, 15));
        capturedInvoice.BillingPeriodStart.Should().Be(new DateTime(2026, 2, 20));
        capturedInvoice.BillingPeriodEnd.Should().Be(new DateTime(2026, 3, 20));
    }

    [Fact]
    public async Task Scenario1_FirstCycle_BillingWindow_Day0_ShouldCreate()
    {
        // Arrange: Cron runs exactly on StartDate (day 0 of window)
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario1_FirstCycle_BillingWindow_Day2_ShouldCreate()
    {
        // Arrange: Cron runs 2 days after StartDate (last day of window)
        var logicalDate = new DateTime(2026, 1, 22);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
    }

    [Fact]
    public async Task Scenario1_FirstCycle_BillingWindow_Day3_ShouldNotCreate()
    {
        // Arrange: Cron runs 3 days after StartDate (outside window)
        var logicalDate = new DateTime(2026, 1, 23);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        _invoiceRepositoryMock.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Never);
    }

    #endregion

    #region Scenario 2: StartDate <= BillingDay (Charge Before Course Starts)

    [Fact]
    public async Task Scenario2_FirstCycle_ShouldTriggerOnBillingDay()
    {
        // Arrange: StartDate = 10, BillingDay = 15
        // First cycle: 10/01 -> 10/02
        // Expected: effectiveTriggerDate = 15/01 (BillingDay)
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 5));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 1, 15));
        capturedInvoice.Status.Should().Be(InvoiceStatus.Scheduled,
            "Enrollment before billing day is Scheduled");
        capturedInvoice.BillingPeriodStart.Should().Be(new DateTime(2026, 1, 10));
        capturedInvoice.BillingPeriodEnd.Should().Be(new DateTime(2026, 2, 10));
    }

    [Fact]
    public async Task Scenario2_FirstCycle_EnrollmentAfterBillingDay_ShouldBeOutstanding()
    {
        // Arrange: Enrollment after billing day
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 20));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 1, 20));
        capturedInvoice.Status.Should().Be(InvoiceStatus.Outstanding,
            "Enrollment after billing day is Outstanding");
    }

    [Fact]
    public async Task Scenario2_SecondCycle_ShouldTriggerOnBillingDay()
    {
        // Arrange: Second cycle (10/02 -> 10/03)
        // Expected: effectiveTriggerDate = 15/02 (BillingDay)
        var logicalDate = new DateTime(2026, 2, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 5));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 2, 15));
        capturedInvoice.Status.Should().Be(InvoiceStatus.Scheduled);
        capturedInvoice.BillingPeriodStart.Should().Be(new DateTime(2026, 2, 10));
        capturedInvoice.BillingPeriodEnd.Should().Be(new DateTime(2026, 3, 10));
    }

    [Fact]
    public async Task Scenario2_AllCycles_ShouldUseSameTriggerLogic()
    {
        // Arrange: Verify consistency across cycles
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 5));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        var capturedInvoices = new List<Invoice>();
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoices.Add(invoice))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act - Cycle 1
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(new DateTime(2026, 1, 15));
        
        // Act - Cycle 2
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(new DateTime(2026, 2, 15));

        // Assert
        capturedInvoices.Should().HaveCount(2);
        capturedInvoices[0].BillingDate.Should().Be(new DateTime(2026, 1, 15));
        capturedInvoices[1].BillingDate.Should().Be(new DateTime(2026, 2, 15));
        capturedInvoices.ForEach(inv => inv.Status.Should().Be(InvoiceStatus.Scheduled));
    }

    #endregion

    #region Billing Date Resolution Edge Cases

    [Fact]
    public void ResolveBillingDate_WhenBillingDayInFirstMonth_ShouldReturnThatDate()
    {
        // Arrange: Period 10/01 -> 10/04, BillingDay = 15
        // Expected: 15/01 (first occurrence in period)
        var periodStart = new DateTime(2026, 1, 10);
        var periodEnd = new DateTime(2026, 4, 10);
        var course = CreateCourse(billingDate: 15);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, false });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public void ResolveBillingDate_WhenBillingDaySkipsFirstMonth_ShouldReturnSecondMonth()
    {
        // Arrange: Period 20/01 -> 20/04, BillingDay = 15
        // 15/01 < 20/01, so skip to 15/02
        var periodStart = new DateTime(2026, 1, 20);
        var periodEnd = new DateTime(2026, 4, 20);
        var course = CreateCourse(billingDate: 15);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, false });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 2, 15));
    }

    [Fact]
    public void ResolveBillingDate_WhenBillingDay31InFebruary_ShouldUseFeb28()
    {
        // Arrange: Period 01/02 -> 01/05, BillingDay = 31
        // Feb has only 28 days in 2026
        var periodStart = new DateTime(2026, 2, 1);
        var periodEnd = new DateTime(2026, 5, 1);
        var course = CreateCourse(billingDate: 31);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, false });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 2, 28));
    }

    [Fact]
    public void ResolveBillingDate_WhenBillingDay31InApril_ShouldUseApril30()
    {
        // Arrange: April has only 30 days
        var periodStart = new DateTime(2026, 4, 1);
        var periodEnd = new DateTime(2026, 7, 1);
        var course = CreateCourse(billingDate: 31);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, false });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 4, 30));
    }

    [Fact]
    public void ResolveBillingDate_FirstCycle_WhenBillingDayLessThanCourseStartDay_ShouldUseCourseStartDate()
    {
        // Arrange: BillingDay = 5, CourseStart = 15th
        // First cycle should return course start date
        var course = CreateCourse(
            billingDate: 5,
            startDate: new DateTime(2026, 1, 15));
        var periodStart = new DateTime(2026, 1, 15);
        var periodEnd = new DateTime(2026, 2, 15);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, true });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 1, 15), "First cycle with billing day < course start day should use course start date");
    }

    [Fact]
    public void ResolveBillingDate_SecondCycle_WhenBillingDayLessThanCourseStartDay_ShouldUseBillingDay()
    {
        // Arrange: BillingDay = 5, CourseStart = 15th
        // Second cycle should use billing day normally
        var course = CreateCourse(
            billingDate: 5,
            startDate: new DateTime(2026, 1, 15));
        var periodStart = new DateTime(2026, 2, 15);
        var periodEnd = new DateTime(2026, 3, 15);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, false });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 3, 5), "Second cycle should use billing day normally");
    }

    [Fact]
    public void ResolveBillingDate_FirstCycle_WhenBillingDayGreaterThanCourseStartDay_ShouldUseBillingDay()
    {
        // Arrange: BillingDay = 20, CourseStart = 10th (happy case)
        var course = CreateCourse(
            billingDate: 20,
            startDate: new DateTime(2026, 1, 10));
        var periodStart = new DateTime(2026, 1, 10);
        var periodEnd = new DateTime(2026, 2, 10);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, true });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 1, 20), "Happy case: billing day >= course start day should use billing day");
    }

    [Fact]
    public void ResolveBillingDate_FirstCycle_CourseStartLastDayOfMonth_BillingDaySmaller_ShouldUseCourseStart()
    {
        // Arrange: CourseStart = 31st, BillingDay = 5
        var course = CreateCourse(
            billingDate: 5,
            startDate: new DateTime(2026, 1, 31));
        var periodStart = new DateTime(2026, 1, 31);
        var periodEnd = new DateTime(2026, 2, 28);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, true });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 1, 31), "Edge case: course starts on 31st, billing day 5 should use course start");
    }

    [Fact]
    public void ResolveBillingDate_FirstCycle_CourseStartDay1_AnyBillingDay_ShouldUseBillingDay()
    {
        // Arrange: CourseStart = 1st, BillingDay = 15
        // billingDay (15) < StartDate.Day (1) is FALSE, so use while loop
        var course = CreateCourse(
            billingDate: 15,
            startDate: new DateTime(2026, 2, 1));
        var periodStart = new DateTime(2026, 2, 1);
        var periodEnd = new DateTime(2026, 3, 1);

        // Act
        var method = typeof(InvoiceService).GetMethod("ResolveBillingDate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var billingDate = (DateTime)method.Invoke(null, new object[] { course, periodStart, periodEnd, true });

        // Assert
        billingDate.Should().Be(new DateTime(2026, 2, 15), "Course starts on 1st, any billing day should be found in period");
    }

    #endregion

    #region OneTime Payment Tests

    [Fact]
    public async Task OneTime_ShouldUseFullCourseRange()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 12, 31),
            billingDate: 15,
            feePerCycle: 500m,
            paymentType: PaymentType.OneTime);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 5));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.Amount.Should().Be(500m);
        capturedInvoice.BillingPeriodStart.Should().Be(new DateTime(2026, 1, 1));
        capturedInvoice.BillingPeriodEnd.Should().Be(new DateTime(2026, 12, 31));
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public async Task OneTime_WhenBillingDayBeforeStartDate_UsesStartDate()
    {
        // Arrange: BillingDay = 5, StartDate = 10
        var logicalDate = new DateTime(2026, 1, 10);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            endDate: new DateTime(2026, 12, 31),
            billingDate: 5,
            feePerCycle: 200m,
            paymentType: PaymentType.OneTime);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 10));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 1, 10));
    }

    [Fact]
    public async Task OneTime_ShouldNotCreateDuplicate()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 10);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 5,
            feePerCycle: 400m,
            paymentType: PaymentType.OneTime);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 10));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((i) => captured = i)
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act - First run
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);
        captured.Should().NotBeNull();

        // Simulate existing invoice by updating the mock to return it
        _invoiceRepositoryMock
            .Setup(r => r.ToListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<int>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice> { captured });

        _invoiceRepositoryMock.Invocations.Clear();

        // Act - Second run
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        _invoiceRepositoryMock.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Never);
    }

    #endregion

    #region Multiple Enrollments Tests

    [Fact]
    public async Task MultipleEnrollments_ShouldCreateInvoiceForEach()
    {
        // Arrange: Trigger 20/01. Cron runs 20/01.
        // billing day (15) < course start day (20) → billingDate = 20/01
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            feePerCycle: 150m,
            paymentType: PaymentType.Recurring);

        var enrollment1 = CreateEnrollment(id: "enroll-001", enrollDate: new DateTime(2026, 1, 15));
        var enrollment2 = CreateEnrollment(id: "enroll-002", enrollDate: new DateTime(2026, 1, 18));
        var enrollment3 = CreateEnrollment(id: "enroll-003", enrollDate: new DateTime(2026, 2, 25)); // After periodEnd, shouldn't be picked up

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment1, enrollment2, enrollment3 });

        var capturedInvoices = new List<Invoice>();
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoices.Add(invoice))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoices.Should().HaveCount(2);
    }

    [Fact]
    public async Task MultipleEnrollments_MixedStatuses_ShouldAssignCorrectly()
    {
        // Arrange: Start 20/01. Trigger 20/01. 
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            paymentType: PaymentType.Recurring);

        var enrollBefore = CreateEnrollment(id: "e-before", enrollDate: new DateTime(2026, 1, 15));
        var enrollOn = CreateEnrollment(id: "e-on", enrollDate: new DateTime(2026, 1, 20));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollBefore, enrollOn });

        var capturedInvoices = new List<Invoice>();
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoices.Add(invoice))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoices.Should().HaveCount(2);
        capturedInvoices.First(i => i.EnrollmentID == "e-before").Status.Should().Be(InvoiceStatus.Scheduled);
        capturedInvoices.First(i => i.EnrollmentID == "e-on").Status.Should().Be(InvoiceStatus.Outstanding);
    }

    #endregion

    #region Payment Due Tests

    [Fact]
    public async Task PaymentDue_ShouldCalculateCorrectDueDate()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            billingDate: 1,
            paymentDue: 10,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 20));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.BillingDate.Should().Be(new DateTime(2026, 1, 20));
        capturedInvoice.DueDate.Should().Be(new DateTime(2026, 1, 30));
    }

    [Fact]
    public async Task PaymentDue_WhenNull_ShouldHaveNullDueDate()
    {
        // Arrange: Trigger 20/01. Cron runs 20/01. 
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 20),
            billingDate: 15,
            paymentDue: null,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.DueDate.Should().BeNull();
    }

    #endregion

    #region Duplicate Prevention Tests

    [Fact]
    public async Task ShouldNotCreateDuplicate_WhenInvoiceExistsForPeriod()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 1);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            billingDate: 1,
            paymentType: PaymentType.Recurring);
        var existing = new Invoice { Id = "inv-1", EnrollmentID = "enroll-001", BillingPeriodStart = new DateTime(2026, 1, 1) };
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2025, 12, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        // Override to return existing invoice
        _invoiceRepositoryMock
            .Setup(r => r.ToListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Invoice, bool>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IQueryable<Invoice>>>(),
                It.IsAny<Func<IQueryable<Invoice>, IOrderedQueryable<Invoice>>>(),
                It.IsAny<int>(),
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Invoice> { existing });

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        _invoiceRepositoryMock.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Never);
    }

    #endregion

    #region Billing Cycles Tests

    [Fact]
    public async Task QuarterlyBillingCycle_ShouldCalculateCorrectPeriod()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 1);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            billingCycle: "Quarterly",
            billingDate: 1,
            feePerCycle: 300m,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2025, 12, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice capturedInvoice = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((invoice) => capturedInvoice = invoice)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveAsync())
            .Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        capturedInvoice.Should().NotBeNull();
        capturedInvoice.Amount.Should().Be(300m);
        capturedInvoice.BillingPeriodStart.Should().Be(new DateTime(2026, 1, 1));
        capturedInvoice.BillingPeriodEnd.Should().Be(new DateTime(2026, 4, 1));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task NoEnrollments_ShouldNotCreateInvoice()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 1);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            billingDate: 1,
            paymentType: PaymentType.Recurring);

        SetupMocks(new List<Course> { course }, new List<Enrollment>());

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        _invoiceRepositoryMock.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task EnrollmentOnBillingDay_ShouldBeOutstanding()
    {
        // Arrange: Enrollment exactly on billing day
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.Status.Should().Be(InvoiceStatus.Outstanding,
            "Enrollment on trigger date should be Outstanding");
    }

    [Fact]
    public async Task BillingWindow_Retry_ShouldWork()
    {
        // Arrange: Test retry within window
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            billingDate: 10,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2025, 12, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice cap1 = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((i) => cap1 = i)
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act - Day +1
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(new DateTime(2026, 1, 11));
        cap1.Should().NotBeNull();

        // Reset for Day +2
        _invoiceRepositoryMock.Invocations.Clear();
        Invoice cap2 = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((i) => cap2 = i)
            .Returns(Task.CompletedTask);

        await _invoiceService.GenerateInvoiceForEnrollmentAsync(new DateTime(2026, 1, 12));
        cap2.Should().NotBeNull();
    }

    [Fact]
    public async Task CourseEndDate_ShouldPreventInvoiceCreation()
    {
        // Arrange: Run date after course end
        var logicalDate = new DateTime(2026, 2, 1);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 1),
            endDate: new DateTime(2026, 1, 31),
            billingDate: 1,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2025, 12, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        _invoiceRepositoryMock.Verify(r => r.InsertAsync(It.IsAny<Invoice>()), Times.Never);
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public async Task Status_EnrollmentBeforeTrigger_ShouldBeScheduled()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 5));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.Status.Should().Be(InvoiceStatus.Scheduled);
    }

    [Fact]
    public async Task Status_EnrollmentAfterTrigger_ShouldBeOutstanding()
    {
        // Arrange
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 20));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.Status.Should().Be(InvoiceStatus.Outstanding);
    }

    #endregion

    #region First Cycle Billing Day Less Than Course Start Day Tests

    [Fact]
    public async Task FirstCycle_BillingDayLessThanCourseStartDay_ShouldUseCourseStartDateAsBillingDate()
    {
        // Arrange: BillingDay = 5, CourseStart = 15th
        // Expected: billingDate = 15/01 (course start), not 05/02
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 15),
            billingDate: 5,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 10));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.BillingDate.Should().Be(new DateTime(2026, 1, 15),
            "First cycle with billing day < course start day should use course start date");
        captured.BillingPeriodStart.Should().Be(new DateTime(2026, 1, 15));
    }

    [Fact]
    public async Task SecondCycle_BillingDayLessThanCourseStartDay_ShouldUseBillingDayNormally()
    {
        // Arrange: BillingDay = 5, CourseStart = 15th
        // Second cycle: 15/02 -> 15/03, billing date should be 05/03
        var logicalDate = new DateTime(2026, 3, 5);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 15),
            billingDate: 5,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 10));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.BillingDate.Should().Be(new DateTime(2026, 3, 5),
            "Second cycle should use billing day normally");
        captured.BillingPeriodStart.Should().Be(new DateTime(2026, 2, 15));
    }

    [Fact]
    public async Task FirstCycle_BillingDayGreaterThanCourseStartDay_ShouldUseBillingDay()
    {
        // Arrange: BillingDay = 20, CourseStart = 10th (happy case)
        var logicalDate = new DateTime(2026, 1, 20);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 10),
            billingDate: 20,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 5));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.BillingDate.Should().Be(new DateTime(2026, 1, 20),
            "Happy case: billing day >= course start day should use billing day");
    }

    [Fact]
    public async Task FirstCycle_BillingDayEqualsCourseStartDay_ShouldUseBillingDay()
    {
        // Arrange: BillingDay = 15, CourseStart = 15th (edge case: equal)
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 15),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 10));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.BillingDate.Should().Be(new DateTime(2026, 1, 15),
            "Billing day equals course start day should use that date");
    }

    [Fact]
    public async Task FirstCycle_CourseStartLastDayOfMonth_BillingDaySmaller_ShouldUseCourseStart()
    {
        // Arrange: CourseStart = Jan 31, BillingDay = 5
        var logicalDate = new DateTime(2026, 1, 31);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 31),
            endDate: new DateTime(2026, 6, 30),
            billingDate: 5,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 25));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.BillingDate.Should().Be(new DateTime(2026, 1, 31),
            "Course starts on 31st, billing day 5 should use course start for first cycle");
    }

    [Fact]
    public async Task FirstCycle_BillingDayLessThanCourseStartDay_EnrollmentBeforeCourseStart_ShouldBeScheduled()
    {
        // Arrange: Enrollment before course start, billing day < course start day
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 15),
            billingDate: 5,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 10));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.Status.Should().Be(InvoiceStatus.Scheduled,
            "Enrollment before effective trigger date should be Scheduled");
    }

    [Fact]
    public async Task FirstCycle_BillingDayLessThanCourseStartDay_EnrollmentOnCourseStart_ShouldBeOutstanding()
    {
        // Arrange: Enrollment on course start date
        var logicalDate = new DateTime(2026, 1, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 15),
            billingDate: 5,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 15));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.Status.Should().Be(InvoiceStatus.Outstanding,
            "Enrollment on or after effective trigger date should be Outstanding");
    }

    [Fact]
    public async Task MultipleCycles_BillingDayLessThanCourseStartDay_ShouldTransitionCorrectly()
    {
        // Arrange: Test transition from first cycle (uses course start) to second cycle (uses billing day)
        var course = CreateCourse(
            startDate: new DateTime(2026, 1, 15),
            billingDate: 5,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 10));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        var capturedInvoices = new List<Invoice>();
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => capturedInvoices.Add(inv))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act - First cycle
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(new DateTime(2026, 1, 15));

        // Act - Second cycle
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(new DateTime(2026, 3, 5));

        // Assert
        capturedInvoices.Should().HaveCount(2);
        
        // First cycle: billing date = course start date
        capturedInvoices[0].BillingDate.Should().Be(new DateTime(2026, 1, 15));
        capturedInvoices[0].BillingPeriodStart.Should().Be(new DateTime(2026, 1, 15));
        
        // Second cycle: billing date = normal billing day
        capturedInvoices[1].BillingDate.Should().Be(new DateTime(2026, 3, 5));
        capturedInvoices[1].BillingPeriodStart.Should().Be(new DateTime(2026, 2, 15));
    }

    [Fact]
    public async Task FirstCycle_CourseStartDay1_BillingDay15_ShouldUseBillingDay()
    {
        // Arrange: CourseStart = 1st, BillingDay = 15
        // billingDay (15) < StartDate.Day (1) is FALSE, so should use billing day
        var logicalDate = new DateTime(2026, 2, 15);
        var course = CreateCourse(
            startDate: new DateTime(2026, 2, 1),
            billingDate: 15,
            paymentType: PaymentType.Recurring);
        var enrollment = CreateEnrollment(enrollDate: new DateTime(2026, 1, 25));

        SetupMocks(new List<Course> { course }, new List<Enrollment> { enrollment });

        Invoice captured = null;
        _invoiceRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<Invoice>()))
            .Callback<Invoice>((inv) => captured = inv)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveAsync()).Returns(Task.CompletedTask);

        // Act
        await _invoiceService.GenerateInvoiceForEnrollmentAsync(logicalDate);

        // Assert
        captured.Should().NotBeNull();
        captured.BillingDate.Should().Be(new DateTime(2026, 2, 15),
            "Course starts on 1st, billing day 15 should use billing day");
    }

    #endregion
}
