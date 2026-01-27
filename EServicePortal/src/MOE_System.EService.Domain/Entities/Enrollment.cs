using MOE_System.EService.Domain.Enums;

namespace MOE_System.EService.Domain.Entities;

public class Enrollment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CourseId { get; set; } = string.Empty;
    public string EducationAccountId { get; set; } = string.Empty;
    public DateTime EnrollDate { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Scheduled;

    // Navigation properties
    public Course? Course { get; set; }
    public EducationAccount? EducationAccount { get; set; }
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
