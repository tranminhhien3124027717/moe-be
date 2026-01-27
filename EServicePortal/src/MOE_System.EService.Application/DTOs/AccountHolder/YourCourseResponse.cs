using MOE_System.EService.Application.Common;

namespace MOE_System.EService.Application.DTOs.AccountHolder
{
    #region Base Class
    
    public abstract class CourseBaseInfo
    {
        public string CourseName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
    }
    
    #endregion

    #region Summary
    
    public class CourseSummaryResponse
    {
        public decimal OutstandingFees { get; set; }
        public decimal Balance { get; set; }
        public int TotalEnrolledCourses { get; set; }
        public int TotalPendingInvoices { get; set; }
    }
    
    #endregion

    #region Enrolled Courses
    
    public class EnrolledCoursesRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class EnrolledCoursesResponse
    {
        public PaginatedList<EnrolledCourse> Courses { get; set; } = null!;
    }

    public class EnrolledCourse : CourseBaseInfo
    {
        public string EnrollmentId { get; set; } = string.Empty;
        public decimal CourseFee { get; set; }
        public string EnrolledDate { get; set; } = string.Empty;
        public string BillingDate { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
    
    #endregion

    #region Pending Fees
    
    public class PendingFeesRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PendingFeesResponse
    {
        public PaginatedList<PendingFees> Fees { get; set; } = null!;
    }

    public class PendingFees : CourseBaseInfo
    {
        public string InvoiceId { get; set; } = string.Empty;
        public decimal AmountDue { get; set; }
        public string BillingDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
    
    #endregion

    #region Payment History
    
    public class PaymentHistoryRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PaymentHistoryResponse
    {
        public PaginatedList<PaymentHistory> History { get; set; } = null!;
    }

    public class PaymentHistory : CourseBaseInfo
    {
        public string InvoiceId { get; set; } = string.Empty;

        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;

        public List<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }

    public class PaymentTransaction
    {
        public decimal Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string PaymentDate { get => TransactionDate?.ToString("dd/MM/yyyy") ?? "-"; set { } }
        public int? PaymentMethodRaw { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }
    
    #endregion

    #region Course Details (by EnrollmentId)

    // Part 1: Course Information
    public class CourseInformationResponse
    {
        public string CourseName { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string CourseStart { get; set; } = string.Empty;
        public string CourseEnd { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string EnrolledDate {  get; set; } = string.Empty;
        public string EducationLevel { get; set; } = string.Empty;
        public decimal FeePerCycle { get; set; }
        public decimal TotalOutstandingFee { get; set; }
        public decimal CourseTotalFee { get; set; }
    }

    // Part 2: Outstanding Fees (Paginated)
    public class OutstandingFeesRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class OutstandingFeesResponse
    {
        public PaginatedList<OutstandingFeeItem> Fees { get; set; } = null!;
    }

    public class OutstandingFeeItem : CourseBaseInfo
    {
        public string InvoiceId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BillingDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public int DaysUntilDue { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    // Part 3: Upcoming Billing Cycles (Paginated)
    public class UpcomingBillingCyclesRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class UpcomingBillingCyclesResponse
    {
        public PaginatedList<UpcomingBillingCycleItem> BillingCycles { get; set; } = null!;
    }

    public class UpcomingBillingCycleItem
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string DueMonth { get; set; } = string.Empty; 
        public string BillingDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty; 
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    // Part 4: Payment History for specific enrollment (Paginated)
    public class EnrollmentPaymentHistoryRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class EnrollmentPaymentHistoryResponse
    {
        public PaginatedList<EnrollmentPaymentHistoryItem> PaymentHistory { get; set; } = null!;
    }

    public class EnrollmentPaymentHistoryItem : CourseBaseInfo
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string PaidCycle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public List<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
    }

    #endregion
}
