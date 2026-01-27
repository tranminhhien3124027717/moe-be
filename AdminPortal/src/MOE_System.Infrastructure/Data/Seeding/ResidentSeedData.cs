using System;
using System.Collections.Generic;
using MOE_System.Domain.Entities;

namespace MOE_System.Infrastructure.Data.Seeding
{
    public static class ResidentSeedData
    {
        /// <summary>
        /// Get a predefined list of 3 residents
        /// </summary>
        /// <param name="count">Number of residents to generate (parameter kept for compatibility, returns fixed 3 residents)</param>
        /// <returns>List of Resident objects</returns>
        public static List<Resident> GetResidents(int count = 3)
        {
            var residents = new List<Resident>
            {
                new Resident
                {
                    NRIC = "T0375353G",
                    PrincipalName = "James Chu",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(2003, 8, 3),
                    ResidentialStatus = "SingaporeCitizen",
                    Nationality = "Singaporean",
                    Country = "Singapore",
                    MobileNumber = "+6591234501",
                    EmailAddress = "james.chu@gmail.com",
                    RegisteredAddress = "Blk 654 Tampines St 61 #12-655"
                },
                new Resident
                {
                    NRIC = "G0360687U",
                    PrincipalName = "Cullen Do",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(2003, 6, 6),
                    ResidentialStatus = "PermanentResident",
                    Nationality = "Singaporean",
                    Country = "Singapore",
                    MobileNumber = "+6591234502",
                    EmailAddress = "cullen.do@gmail.com",
                    RegisteredAddress = "Blk 654 Tampines St 61 #12-656"
                },
                new Resident
                {
                    NRIC = "F9890160N",
                    PrincipalName = "Kay Ngo",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(1998, 12, 18),
                    ResidentialStatus = "NonResident",
                    Nationality = "Vietnamese",
                    Country = "Vietnam",
                    MobileNumber = "+6591234503",
                    EmailAddress = "kay.ngo@gmail.com",
                    RegisteredAddress = "Blk 654 Tampines St 61 #12-657"
                },
                new Resident
                {
                    NRIC = "T1005124A",
                    PrincipalName = "Damian Nguyen",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(2010, 9, 21),
                    ResidentialStatus = "SingaporeCitizen",
                    Nationality = "Singaporean",
                    Country = "Singapore",
                    MobileNumber = "+6591234504",
                    EmailAddress = "damian.nguyen@gmail.com",
                    RegisteredAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123"
                },
                new Resident
                {
                    NRIC = "S9607567E",
                    PrincipalName = "Tracy Tran",
                    Sex = "F",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(1996, 2, 1),
                    ResidentialStatus = "SingaporeCitizen",
                    Nationality = "Singaporean",
                    Country = "Singapore",
                    MobileNumber = "+6591234505",
                    EmailAddress = "tracy.tran@gmail.com",
                    RegisteredAddress = "Blk 654 Tampines St 61 #12-654"
                },
                new Resident
                {
                    NRIC = "F9716779Q",
                    PrincipalName = "Jason Nguyen",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(1997, 2, 2),
                    ResidentialStatus = "NonResident",
                    Nationality = "Vietnamese",
                    Country = "Vietnam",
                    MobileNumber = "+6591234506",
                    EmailAddress = "jason.nguyen@gmail.com",
                    RegisteredAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123"
                },









        //=========================Account already exsisted in system========================
                new Resident
                {
                    NRIC = "S9976551H",
                    PrincipalName = "Eric Nguyen",
                    Sex = "M",
                    Race = "Raumanian",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(1999, 6, 25),
                    ResidentialStatus = "SingaporeCitizen",
                    Nationality = "Raumanian",
                    Country = "Raumania",
                    MobileNumber = "+6591234501",
                    EmailAddress = "eric.nguyen@gmail.com",
                    RegisteredAddress = "Blk 789 Jurong West St 52 #03-789"
                },
                new Resident
                {
                    NRIC = "T0293092A",
                    PrincipalName = "Dave Dao",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(2002, 8, 15),
                    ResidentialStatus = "SingaporeCitizen",
                    Nationality = "Vietnamese",
                    Country = "Vietnam",
                    MobileNumber = "+6591234502",
                    EmailAddress = "dave.dao@gmail.com",
                    RegisteredAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123"
                },
                new Resident
                {
                    NRIC = "T0462282G",
                    PrincipalName = "Kain Tran",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(2004, 8, 14),
                    ResidentialStatus = "SingaporeCitizen",
                    Nationality = "Vietnamese",
                    Country = "Vietnam",
                    MobileNumber = "+6591234503",
                    EmailAddress = "kain.tran@gmail.com",
                    RegisteredAddress = "Blk 456 Bedok North St 1 #10-456"
                },
                new Resident
                {
                    NRIC = "T0234567A",
                    PrincipalName = "Ryan Le",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(2002, 12, 15),
                    ResidentialStatus = "PermanentResident",
                    Nationality = "Vietnamese",
                    Country = "Vietnam",
                    MobileNumber = "+6591234504",
                    EmailAddress = "ryan.le@gmail.com",
                    RegisteredAddress = "Blk 456 Bedok North St 1 #10-456"
                },
                new Resident
                {
                    NRIC = "G0290334U",
                    PrincipalName = "Suki Nguyen",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(2002, 12, 15),
                    ResidentialStatus = "NonResident",
                    Nationality = "Vietnamese",
                    Country = "Vietnam",
                    MobileNumber = "+6591234505",
                    EmailAddress = "suki.nguyen@gmail.com",
                    RegisteredAddress = "Blk 123 Ang Mo Kio Ave 3 #05-123"
                },
                new Resident
                {
                    NRIC = "S9486546H",
                    PrincipalName = "Joey Nguyen",
                    Sex = "M",
                    Race = "Chinese",
                    SecondaryRace = string.Empty,
                    Dialect = string.Empty,
                    DateOfBirth = new DateOnly(1994, 12, 15),
                    ResidentialStatus = "SingaporeCitizen",
                    Nationality = "Vietnamese",
                    Country = "Vietnam",
                    MobileNumber = "+6591234506",
                    EmailAddress = "joey.nguyen@gmail.com",
                    RegisteredAddress = "Blk 654 Tampines St 61 #12-654"
                }
            };

            return residents;
        }
    }
}
