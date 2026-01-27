namespace MOE_System.EService.Domain.Enums;

public enum EducationLevel
{
        NotSet = 0,
        Primary = 1,
        Secondary = 2,
        PostSecondary = 3,
        Tertiary = 4,
        PostGraduate = 5
}

public static class EducationLevelExtensions
{
    public static string ToFriendlyString(this EducationLevel level)
    {
        return level switch
        {
            EducationLevel.NotSet => "Not Set",
            EducationLevel.Primary => "Primary",
            EducationLevel.Secondary => "Secondary",
            EducationLevel.PostSecondary => "Post-Secondary",
            EducationLevel.Tertiary => "Tertiary",
            EducationLevel.PostGraduate => "Post-Graduate",
            _ => "Unknown",
        };
    }
}
