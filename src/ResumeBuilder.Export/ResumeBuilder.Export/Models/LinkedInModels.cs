using CsvHelper.Configuration.Attributes;

namespace ResumeBuilder.Export.Models;

/// <summary>
/// LinkedIn Data Export CSV Models
/// Based on LinkedIn's "Download Your Data" export format
/// </summary>

public class LinkedInProfile
{
    [Name("First Name")]
    public string? FirstName { get; set; }

    [Name("Last Name")]
    public string? LastName { get; set; }

    [Name("Maiden Name")]
    public string? MaidenName { get; set; }

    [Name("Address")]
    public string? Address { get; set; }

    [Name("Birth Date")]
    public string? BirthDate { get; set; }

    [Name("Headline")]
    public string? Headline { get; set; }

    [Name("Summary")]
    public string? Summary { get; set; }

    [Name("Industry")]
    public string? Industry { get; set; }

    [Name("Zip Code")]
    public string? ZipCode { get; set; }

    [Name("Geo Location")]
    public string? GeoLocation { get; set; }

    [Name("Twitter Handles")]
    public string? TwitterHandles { get; set; }

    [Name("Websites")]
    public string? Websites { get; set; }

    [Name("Instant Messengers")]
    public string? InstantMessengers { get; set; }
}

public class LinkedInEmail
{
    [Name("Email Address")]
    public string? EmailAddress { get; set; }

    [Name("Confirmed")]
    public string? Confirmed { get; set; }

    [Name("Primary")]
    public string? Primary { get; set; }

    [Name("Updated On")]
    public string? UpdatedOn { get; set; }
}

public class LinkedInPhoneNumber
{
    [Name("Number")]
    public string? Number { get; set; }

    [Name("Extension")]
    public string? Extension { get; set; }

    [Name("Type")]
    public string? Type { get; set; }
}

public class LinkedInPosition
{
    [Name("Company Name")]
    public string? CompanyName { get; set; }

    [Name("Title")]
    public string? Title { get; set; }

    [Name("Description")]
    public string? Description { get; set; }

    [Name("Location")]
    public string? Location { get; set; }

    [Name("Started On")]
    public string? StartedOn { get; set; }

    [Name("Finished On")]
    public string? FinishedOn { get; set; }
}

public class LinkedInEducation
{
    [Name("School Name")]
    public string? SchoolName { get; set; }

    [Name("Start Date")]
    public string? StartDate { get; set; }

    [Name("End Date")]
    public string? EndDate { get; set; }

    [Name("Notes")]
    public string? Notes { get; set; }

    [Name("Degree Name")]
    public string? DegreeName { get; set; }

    [Name("Activities")]
    public string? Activities { get; set; }
}

public class LinkedInSkill
{
    [Name("Name")]
    public string? Name { get; set; }
}

public class LinkedInCertification
{
    [Name("Name")]
    public string? Name { get; set; }

    [Name("Url")]
    public string? Url { get; set; }

    [Name("Authority")]
    public string? Authority { get; set; }

    [Name("Started On")]
    public string? StartedOn { get; set; }

    [Name("Finished On")]
    public string? FinishedOn { get; set; }

    [Name("License Number")]
    public string? LicenseNumber { get; set; }
}

public class LinkedInLanguage
{
    [Name("Name")]
    public string? Name { get; set; }

    [Name("Proficiency")]
    public string? Proficiency { get; set; }
}

public class LinkedInProject
{
    [Name("Title")]
    public string? Title { get; set; }

    [Name("Description")]
    public string? Description { get; set; }

    [Name("Url")]
    public string? Url { get; set; }

    [Name("Started On")]
    public string? StartedOn { get; set; }

    [Name("Finished On")]
    public string? FinishedOn { get; set; }
}
