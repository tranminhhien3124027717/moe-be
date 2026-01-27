using MOE_System.Domain.Common;
using MOE_System.Domain.Enums;

namespace MOE_System.Domain.Entities;

public class Course : BaseEntity
{
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public decimal FeeAmount { get; set; }
    public decimal? FeePerCycle { get; set; }
    public int DurationByMonth { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; } = PaymentType.OneTime;
    public string? BillingCycle { get; set; }
    
    // Fields from CourseOffering
    public string LearningType { get; set; } = string.Empty;
    public string TermName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    
    // New fields
    public EducationLevel? EducationLevel { get; set; }
    public int? BillingDate { get; set; }
    public int? PaymentDue { get; set; }

    // Navigation properties
    public Provider? Provider { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
