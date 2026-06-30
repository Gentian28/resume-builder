using System.Text.Json.Serialization;

namespace ResumeBuilder.Export.Models;

/// <summary>
/// JSON Resume Standard v1.0.0 Schema
/// https://jsonresume.org/schema/
/// </summary>
public class JsonResumeSchema
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://raw.githubusercontent.com/jsonresume/resume-schema/v1.0.0/schema.json";

    [JsonPropertyName("basics")]
    public JsonResumeBasics? Basics { get; set; }

    [JsonPropertyName("work")]
    public List<JsonResumeWork>? Work { get; set; }

    [JsonPropertyName("education")]
    public List<JsonResumeEducation>? Education { get; set; }

    [JsonPropertyName("skills")]
    public List<JsonResumeSkill>? Skills { get; set; }

    [JsonPropertyName("languages")]
    public List<JsonResumeLanguage>? Languages { get; set; }

    [JsonPropertyName("certificates")]
    public List<JsonResumeCertificate>? Certificates { get; set; }

    [JsonPropertyName("projects")]
    public List<JsonResumeProject>? Projects { get; set; }

    [JsonPropertyName("volunteer")]
    public List<JsonResumeVolunteer>? Volunteer { get; set; }

    [JsonPropertyName("awards")]
    public List<JsonResumeAward>? Awards { get; set; }

    [JsonPropertyName("publications")]
    public List<JsonResumePublication>? Publications { get; set; }

    [JsonPropertyName("interests")]
    public List<JsonResumeInterest>? Interests { get; set; }

    [JsonPropertyName("references")]
    public List<JsonResumeReference>? References { get; set; }

    [JsonPropertyName("meta")]
    public JsonResumeMeta? Meta { get; set; }
}

public class JsonResumeBasics
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("label")]
    public string? Label { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("location")]
    public JsonResumeLocation? Location { get; set; }

    [JsonPropertyName("profiles")]
    public List<JsonResumeProfile>? Profiles { get; set; }
}

public class JsonResumeLocation
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }
}

public class JsonResumeProfile
{
    [JsonPropertyName("network")]
    public string? Network { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class JsonResumeWork
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("highlights")]
    public List<string>? Highlights { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}

public class JsonResumeEducation
{
    [JsonPropertyName("institution")]
    public string? Institution { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("area")]
    public string? Area { get; set; }

    [JsonPropertyName("studyType")]
    public string? StudyType { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("score")]
    public string? Score { get; set; }

    [JsonPropertyName("courses")]
    public List<string>? Courses { get; set; }
}

public class JsonResumeSkill
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("level")]
    public string? Level { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }
}

public class JsonResumeLanguage
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("fluency")]
    public string? Fluency { get; set; }
}

public class JsonResumeCertificate
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

public class JsonResumeProject
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("highlights")]
    public List<string>? Highlights { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }

    [JsonPropertyName("entity")]
    public string? Entity { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class JsonResumeVolunteer
{
    [JsonPropertyName("organization")]
    public string? Organization { get; set; }

    [JsonPropertyName("position")]
    public string? Position { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("highlights")]
    public List<string>? Highlights { get; set; }
}

public class JsonResumeAward
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("awarder")]
    public string? Awarder { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public class JsonResumePublication
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("publisher")]
    public string? Publisher { get; set; }

    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

public class JsonResumeInterest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }
}

public class JsonResumeReference
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }
}

public class JsonResumeMeta
{
    [JsonPropertyName("canonical")]
    public string? Canonical { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("lastModified")]
    public string? LastModified { get; set; }
}
