using MOE_System.Domain.Entities;

namespace MOE_System.Infrastructure.Data.Seeding;

public static class ProviderSeedData
{
    private static void AddLevels(Provider provider, string levels,
        SchoolingLevel? primaryLevel,
        SchoolingLevel? secondaryLevel,
        SchoolingLevel? postSecondaryLevel,
        SchoolingLevel? tertiaryLevel,
        SchoolingLevel? postGraduateLevel)
    {
        var levelList = levels.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());
        
        foreach (var level in levelList)
        {
            switch (level)
            {
                case "Primary":
                    if (primaryLevel != null && !provider.SchoolingLevels.Contains(primaryLevel)) 
                        provider.SchoolingLevels.Add(primaryLevel);
                    break;
                case "Secondary":
                    if (secondaryLevel != null && !provider.SchoolingLevels.Contains(secondaryLevel)) 
                        provider.SchoolingLevels.Add(secondaryLevel);
                    break;
                case "Post-Secondary":
                case "PostSecondary":
                    if (postSecondaryLevel != null && !provider.SchoolingLevels.Contains(postSecondaryLevel)) 
                        provider.SchoolingLevels.Add(postSecondaryLevel);
                    break;
                case "Tertiary":
                    if (tertiaryLevel != null && !provider.SchoolingLevels.Contains(tertiaryLevel)) 
                        provider.SchoolingLevels.Add(tertiaryLevel);
                    break;
                case "Post-Graduate":
                case "PostGraduate":
                    if (postGraduateLevel != null && !provider.SchoolingLevels.Contains(postGraduateLevel)) 
                        provider.SchoolingLevels.Add(postGraduateLevel);
                    break;
            }
        }
    }

    public static List<Provider> GetProvidersForSeeding(
        SchoolingLevel? primaryLevel,
        SchoolingLevel? secondaryLevel,
        SchoolingLevel? postSecondaryLevel,
        SchoolingLevel? tertiaryLevel,
        SchoolingLevel? postGraduateLevel)
    {
        var providers = new List<Provider>();

        // Primary Schools
        var provider = new Provider { Id = "T07GS2001A", Name = "ADMIRALTY PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2002B", Name = "ANCHOR GREEN PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1775F", Name = "ANDERSON PRIMARY SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS2204D", Name = "ANDERSON SERANGOON JUNIOR COLLEGE", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2003C", Name = "ANG MO KIO PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2101A", Name = "ANG MO KIO SECONDARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2004D", Name = "BALESTIER HILL PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS3301A", Name = "BCA ACADEMY", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary,Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2102B", Name = "BEDOK SOUTH SECONDARY SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2103C", Name = "BISHAN PARK SECONDARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2005E", Name = "BLANGAH RISE PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2104D", Name = "BUKIT MERAH SECONDARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1020C", Name = "BUKIT PANJANG PRIMARY SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2006F", Name = "BUKIT TIMAH PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2105E", Name = "BUKIT VIEW SECONDARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS2205E", Name = "CATHOLIC JUNIOR COLLEGE", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2007G", Name = "CHANGKAT PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2106F", Name = "CHIJ SECONDARY (TOA PAYOH)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2008H", Name = "CHONGFU SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2107G", Name = "COMMONWEALTH SECONDARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2108H", Name = "CRESCENT GIRLS' SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2009J", Name = "DA QIAO PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3202B", Name = "DUKE-NUS MEDICAL SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Graduate,Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2109J", Name = "DUNMAN SECONDARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS2206F", Name = "EUNOIA JUNIOR COLLEGE", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS2010K", Name = "EVERGREEN PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS2110K", Name = "FAIRFIELD METHODIST SCHOOL (SECONDARY)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1038J", Name = "HAIG GIRLS' SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS3305E", Name = "INSTITUTE OF TECHNICAL EDUCATION (ITE)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary,Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS3501A", Name = "INTEGRATED PROGRAMME SCHOOL - SAMPLE", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary,Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T08GS3401A", Name = "INTERNATIONAL BACCALAUREATE (IB) DIPLOMA CENTRE", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Secondary,Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1045D", Name = "JURONG PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS3303C", Name = "LASALLE COLLEGE OF THE ARTS", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS2207G", Name = "MERIDIAN JUNIOR COLLEGE (LEGACY DATASET)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS2210K", Name = "MILLENNIA INSTITUTE", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS3304D", Name = "NANYANG ACADEMY OF FINE ARTS (NAFA)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS2304D", Name = "NANYANG POLYTECHNIC", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS3102B", Name = "NANYANG TECHNOLOGICAL UNIVERSITY (NTU)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3201A", Name = "NATIONAL INSTITUTE OF EDUCATION (NIE)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS3101A", Name = "NATIONAL UNIVERSITY OF SINGAPORE (NUS)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS2302B", Name = "NGEE ANN POLYTECHNIC", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3206F", Name = "NTU NANYANG BUSINESS SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3207G", Name = "NTU SCHOOL OF COMPUTER SCIENCE AND ENGINEERING", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3203C", Name = "NUS BUSINESS SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3205E", Name = "NUS YONG LOO LIN SCHOOL OF MEDICINE", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T12GS0008C", Name = "PALM VIEW PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS3502B", Name = "PRIMARY-TO-SECONDARY BRIDGING PROGRAMME PROVIDER - SAMPLE", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary,Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T12GS0002E", Name = "PUNGGOL GREEN PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1071B", Name = "QUEENSTOWN PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1072J", Name = "RADIN MAS PRIMARY SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1073E", Name = "RAFFLES GIRLS' PRIMARY SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS2305E", Name = "REPUBLIC POLYTECHNIC", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T12GS0007G", Name = "RIVERSIDE PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T12GS0004H", Name = "SENGKANG GREEN PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS3302B", Name = "SHATEC INSTITUTES", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary,Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS3104D", Name = "SINGAPORE INSTITUTE OF TECHNOLOGY (SIT)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS3103C", Name = "SINGAPORE MANAGEMENT UNIVERSITY (SMU)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS2301A", Name = "SINGAPORE POLYTECHNIC", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS3105E", Name = "SINGAPORE UNIVERSITY OF SOCIAL SCIENCES (SUSS)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS3106F", Name = "SINGAPORE UNIVERSITY OF TECHNOLOGY AND DESIGN (SUTD)", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3210K", Name = "SIT SCHOOL OF INFORMATION AND DIGITAL TECHNOLOGIES", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3208H", Name = "SMU SCHOOL OF LAW", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T12GS0005D", Name = "SPRINGDALE PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T11GS3209J", Name = "SUTD PILLAR OF ARCHITECTURE AND SUSTAINABLE DESIGN", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary,Post-Graduate", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T07GS1043A", Name = "TECK WHYE PRIMARY SCHOOL", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS2208H", Name = "TEMASEK JUNIOR COLLEGE", Status = "Inactive", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T10GS2303C", Name = "TEMASEK POLYTECHNIC", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Tertiary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T09GS2209J", Name = "VICTORIA JUNIOR COLLEGE", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Post-Secondary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        provider = new Provider { Id = "T12GS0001J", Name = "WESTWOOD PRIMARY SCHOOL", Status = "Active", SchoolingLevels = new List<SchoolingLevel>() };
        AddLevels(provider, "Primary", primaryLevel, secondaryLevel, postSecondaryLevel, tertiaryLevel, postGraduateLevel);
        providers.Add(provider);

        return providers;
    }
}