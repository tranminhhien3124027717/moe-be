namespace MOE_System.Domain.Enums;

public enum ChangeType
{
    TopUp = 0,
    CoursePayment = 1
}

public static class ChangeTypeExtensions
{
    public static string GetKey(this ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.TopUp => "TU",
            ChangeType.CoursePayment => "CP",
            _ => throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null)
        };
    }
}
