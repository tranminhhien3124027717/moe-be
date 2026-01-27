using System;
using System.Collections.Generic;
using System.Text;

namespace MOE_System.EService.Domain.Enums
{
    public enum AccountType
    {
        StudentAccount = 1,
        EducationAccount = 2
    }

    public static class AccountTypeExtensions
    {
        public static string ToFriendlyString(this AccountType accountType)
        {
            return accountType switch
            {
                AccountType.StudentAccount => "Student Account",
                AccountType.EducationAccount => "Education Account",
                _ => "Unknown Account Type"
            };
        }
    }
}
