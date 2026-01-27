using MOE_System.Domain.Common;
using MOE_System.Domain.Enums;

namespace MOE_System.Domain.Entities;

public class AccountHolder : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string RegisteredAddress { get; set; } = string.Empty;
    public string MailingAddress { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty; // Combined address field
    public string Email { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string NRIC { get; set; } = string.Empty;
    public string CitizenId { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string ContLearningStatus { get; set; } = string.Empty;
    public EducationLevel EducationLevel { get; set; } = EducationLevel.Primary;
    public SchoolingStatus SchoolingStatus { get; set; } = SchoolingStatus.NotInSchool;
    public string ResidentialStatus { get; set; } = string.Empty;


    // Navigation property (1-to-1)                                            
    public EducationAccount? EducationAccount { get; set; }
    public ICollection<TopupRuleAccountHolder> TopupRuleAccountHolders { get; set; } = new List<TopupRuleAccountHolder>();

    public bool IsEligibleForAccountClosure(int age, int referenceYear)
    {
        return referenceYear - DateOfBirth.Year >= age;
    }
    public string FullName => $"{FirstName} {LastName}";
}
