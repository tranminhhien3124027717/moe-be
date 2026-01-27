using MOE_System.Domain.Entities;
using MOE_System.Domain.Enums;

namespace MOE_System.Infrastructure.Data.Seeding;

public static class AccountSeedData
{
    public static List<(AccountHolder AccountHolder, EducationAccount EducationAccount)> GetAccountsForSeeding()
    {
        var accounts = new List<(AccountHolder, EducationAccount)>();

        // Eric Nguyen - Singapore Citizen, Secondary
        var ericHolder = new AccountHolder
        {
            Id = "95af3c9c-49ef-4f1c-aa74-7b1ca280f7cf",
            FirstName = "Eric",
            LastName = "Nguyen",
            DateOfBirth = new DateTime(1999, 6, 25),
            RegisteredAddress = "Blk 789 Jurong West St 52 #03-789",
            MailingAddress = "Blk 789 Jurong West St 52 #03-789",
            Address = "Blk 789 Jurong West St 52 #03-789",
            Email = "eric.nguyen@gmail.com",
            ContactNumber = "+6591234501",
            NRIC = "S9976551H",
            CitizenId = "S9976551H",
            Gender = "Male",
            ContLearningStatus = "Active",
            EducationLevel = EducationLevel.Secondary,
            SchoolingStatus = SchoolingStatus.InSchool,
            ResidentialStatus = ResidentialStatus.SingaporeCitizen.ToString()
        };
        var ericAccount = new EducationAccount
        {
            Id = "2793d115-7242-46c9-86e2-93aa565ff435",
            AccountHolderId = ericHolder.Id,
            UserName = "eric.nguyen",
            Password = BCrypt.Net.BCrypt.HashPassword("123"),
            Balance = 4700.00m,
            IsActive = true
        };
        accounts.Add((ericHolder, ericAccount));

        // Dave Dao - Singapore Citizen, PostSecondary
        var daveHolder = new AccountHolder
        {
            Id = "1a5080f4-e948-4121-90ef-b8e9d3f0cafc",
            FirstName = "Dave",
            LastName = "Dao",
            DateOfBirth = new DateTime(2002, 8, 13),
            RegisteredAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123",
            MailingAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123",
            Address = "Blk 123 Ang Mo Kio Ave 3 #05-123",
            Email = "dave.dao@gmail.com",
            ContactNumber = "+6591234502",
            NRIC = "T0293092A",
            CitizenId = "T0293092A",
            Gender = "Male",
            ContLearningStatus = "Active",
            EducationLevel = EducationLevel.PostSecondary,
            SchoolingStatus = SchoolingStatus.InSchool,
            ResidentialStatus = ResidentialStatus.SingaporeCitizen.ToString()
        };
        var daveAccount = new EducationAccount
        {
            Id = "458f6d8b-0e09-4db7-bb9a-10407a0bcb43",
            AccountHolderId = daveHolder.Id,
            UserName = "dave.dao",
            Password = BCrypt.Net.BCrypt.HashPassword("123"),
            Balance = 4700.00m,
            IsActive = true
        };
        accounts.Add((daveHolder, daveAccount));

        // Kain Tran - Singapore Citizen, PostSecondary (Inactive status in data)
        var kainHolder = new AccountHolder
        {
            Id = "0ebe3cd8-3c8e-4ed8-8b41-05ab02784558",
            FirstName = "Kain",
            LastName = "Tran",
            DateOfBirth = new DateTime(2004, 8, 14),
            RegisteredAddress = "Blk 456 Bedok North St 1 #10-456",
            MailingAddress = "Blk 456 Bedok North St 1 #10-456",
            Address = "Blk 456 Bedok North St 1 #10-456",
            Email = "kain.tran@gmail.com",
            ContactNumber = "+6591234503",
            NRIC = "T0462282G",
            CitizenId = "T0462282G",
            Gender = "Male",
            ContLearningStatus = "Active",
            EducationLevel = EducationLevel.PostSecondary,
            SchoolingStatus = SchoolingStatus.InSchool,
            ResidentialStatus = ResidentialStatus.SingaporeCitizen.ToString()
        };
        var kainAccount = new EducationAccount
        {
            Id = "cb50d51a-4b0f-42b7-9994-f06e3c15ea1c",
            AccountHolderId = kainHolder.Id,
            UserName = "kain.tran",
            Password = BCrypt.Net.BCrypt.HashPassword("123"),
            Balance = 4700.00m,
            IsActive = true
        };
        accounts.Add((kainHolder, kainAccount));

        // Ryan Le - Permanent Resident, PostSecondary
        var ryanHolder = new AccountHolder
        {
            Id = "ec7ec480-70e2-4873-93ad-d63d5753b2f1",
            FirstName = "Ryan",
            LastName = "Le",
            DateOfBirth = new DateTime(2002, 12, 15),
            RegisteredAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123",
            MailingAddress = "Blk 456 Bedok North St 1 #10-456",
            Address = "Blk 456 Bedok North St 1 #10-456",
            Email = "ryan.le@gmail.com",
            ContactNumber = "+6591234504",
            NRIC = "T0234567A",
            CitizenId = "T0234567A",
            Gender = "Male",
            ContLearningStatus = "Active",
            EducationLevel = EducationLevel.PostSecondary,
            SchoolingStatus = SchoolingStatus.InSchool,
            ResidentialStatus = ResidentialStatus.PermanentResident.ToString()
        };
        var ryanAccount = new EducationAccount
        {
            Id = "a2e9b432-fd9e-428b-9f36-56c7e1a0b658",
            AccountHolderId = ryanHolder.Id,
            UserName = "ryan.le",
            Password = BCrypt.Net.BCrypt.HashPassword("123"),
            Balance = 0.00m,
            IsActive = true
        };
        accounts.Add((ryanHolder, ryanAccount));

        // Suki Nguyen - Non-Resident, Secondary
        var sukiHolder = new AccountHolder
        {
            Id = "b8f3c9d5-8a6e-4f2c-9b3d-7e4a5c1f2d6a",
            FirstName = "Suki",
            LastName = "Nguyen",
            DateOfBirth = new DateTime(2002, 12, 15),
            RegisteredAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123",
            MailingAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123",
            Address = "Blk 123 Ang Mo Kio Ave 3 #05-123",
            Email = "suki.nguyen@gmail.com",
            ContactNumber = "+6591234505",
            NRIC = "G0290334U",
            CitizenId = "G0290334U",
            Gender = "Female",
            ContLearningStatus = "Active",
            EducationLevel = EducationLevel.Secondary,
            SchoolingStatus = SchoolingStatus.InSchool,
            ResidentialStatus = ResidentialStatus.NonResident.ToString()
        };
        var sukiAccount = new EducationAccount
        {
            Id = "c7d8e9f0-1a2b-3c4d-5e6f-7a8b9c0d1e2f",
            AccountHolderId = sukiHolder.Id,
            UserName = "suki.nguyen",
            Password = BCrypt.Net.BCrypt.HashPassword("123"),
            Balance = 0.00m,
            IsActive = true
        };
        accounts.Add((sukiHolder, sukiAccount));

        // Joey Nguyen - Singapore Citizen, PostSecondary, DEACTIVATED
        var joeyHolder = new AccountHolder
        {
            Id = "d9e0f1a2-3b4c-5d6e-7f8a-9b0c1d2e3f4a",
            FirstName = "Joey",
            LastName = "Nguyen",
            DateOfBirth = new DateTime(1994, 12, 15),
            RegisteredAddress = "Blk 654 Tampines St 61 #12-654",
            MailingAddress = "Blk 654 Tampines St 61 #12-654",
            Address = "Blk 654 Tampines St 61 #12-654",
            Email = "joey.nguyen@gmail.com",
            ContactNumber = "+6591234506",
            NRIC = "S9486546H",
            CitizenId = "S9486546H",
            Gender = "Female",
            ContLearningStatus = "Inactive",
            EducationLevel = EducationLevel.PostSecondary,
            SchoolingStatus = SchoolingStatus.InSchool,
            ResidentialStatus = ResidentialStatus.SingaporeCitizen.ToString()
        };
        var joeyAccount = new EducationAccount
        {
            Id = "e0f1a2b3-4c5d-6e7f-8a9b-0c1d2e3f4a5b",
            AccountHolderId = joeyHolder.Id,
            UserName = "joey.nguyen",
            Password = BCrypt.Net.BCrypt.HashPassword("123"),
            Balance = 5000.00m,
            IsActive = false
        };
        accounts.Add((joeyHolder, joeyAccount));

        return accounts;
    }
}
