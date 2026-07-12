using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.Validation;

public class ResumeValidator
{
    /// <summary>Kept in step with the Summary column length in ResumeDbContext.</summary>
    public const int SummaryMaxLength = 5000;

    public ResumeValidationResult Validate(Resume resume)
    {
        var result = new ResumeValidationResult();

        ValidatePersonalInfo(resume.PersonalInfo, result);

        if (!string.IsNullOrWhiteSpace(resume.Summary) && resume.Summary.Length > SummaryMaxLength)
        {
            result.AddError("Summary", $"Summary cannot exceed {SummaryMaxLength} characters");
        }

        foreach (var exp in resume.Experiences)
        {
            ValidateExperience(exp, result);
        }

        foreach (var edu in resume.EducationList)
        {
            ValidateEducation(edu, result);
        }

        foreach (var skill in resume.Skills)
        {
            ValidateSkill(skill, result);
        }

        foreach (var language in resume.Languages)
        {
            ValidateLanguage(language, result);
        }

        foreach (var certification in resume.Certifications)
        {
            ValidateCertification(certification, result);
        }

        foreach (var project in resume.Projects)
        {
            ValidateProject(project, result);
        }

        foreach (var section in resume.CustomSections)
        {
            ValidateCustomSection(section, result);
        }

        return result;
    }

    private void ValidateSkill(Skill skill, ResumeValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(skill.Name))
        {
            result.AddWarning("Skills", "Skill entry has no name and will not be shown");
        }
    }

    private void ValidateLanguage(Language language, ResumeValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(language.Name))
        {
            result.AddWarning("Languages", "Language entry has no name and will not be shown");
        }
    }

    private void ValidateCertification(Certification certification, ResumeValidationResult result)
    {
        var label = string.IsNullOrWhiteSpace(certification.Name)
            ? "Certifications"
            : $"Certification: {certification.Name}";

        if (string.IsNullOrWhiteSpace(certification.Name))
        {
            result.AddWarning("Certifications", "Certification entry has no name and will not be shown");
        }

        if (!certification.DoesNotExpire)
        {
            var dateRange = new DateRangeRule(certification.IssueDate, certification.ExpirationDate).Validate(null);
            if (!dateRange.IsValid)
            {
                result.AddError(label, "Issue date must be before expiration date");
            }
        }

        if (!string.IsNullOrWhiteSpace(certification.CredentialUrl))
        {
            var url = new UrlRule().Validate(certification.CredentialUrl);
            if (!url.IsValid)
            {
                result.AddError(label, url.ErrorMessage!);
            }
        }
    }

    private void ValidateProject(Project project, ResumeValidationResult result)
    {
        var label = string.IsNullOrWhiteSpace(project.Name) ? "Projects" : $"Project: {project.Name}";

        if (string.IsNullOrWhiteSpace(project.Name))
        {
            result.AddWarning("Projects", "Project entry has no name and will not be shown");
        }

        if (!project.IsOngoing)
        {
            var dateRange = new DateRangeRule(project.StartDate, project.EndDate).Validate(null);
            if (!dateRange.IsValid)
            {
                result.AddError(label, "Start date must be before end date");
            }
        }

        if (!string.IsNullOrWhiteSpace(project.Url))
        {
            var url = new UrlRule().Validate(project.Url);
            if (!url.IsValid)
            {
                result.AddError(label, url.ErrorMessage!);
            }
        }
    }

    private void ValidateCustomSection(CustomSection section, ResumeValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(section.Title))
        {
            result.AddWarning("Custom Sections", "Custom section has no title and will not be shown");
        }

        foreach (var item in section.Items)
        {
            var dateRange = new DateRangeRule(item.StartDate, item.EndDate).Validate(null);
            if (!dateRange.IsValid)
            {
                result.AddError($"Custom Section: {section.Title}", "Start date must be before end date");
            }
        }
    }

    private void ValidatePersonalInfo(PersonalInfo info, ResumeValidationResult result)
    {
        // Required fields
        if (string.IsNullOrWhiteSpace(info.FirstName) && string.IsNullOrWhiteSpace(info.LastName))
        {
            result.AddError("Name", "Name is required");
        }

        // Email validation
        if (!string.IsNullOrWhiteSpace(info.Email))
        {
            var emailRule = new EmailRule();
            var emailResult = emailRule.Validate(info.Email);
            if (!emailResult.IsValid)
            {
                result.AddError("Email", emailResult.ErrorMessage!);
            }
        }
        else
        {
            result.AddWarning("Email", "Email is recommended for contact");
        }

        // Phone validation
        if (!string.IsNullOrWhiteSpace(info.Phone))
        {
            var phoneRule = new PhoneRule();
            var phoneResult = phoneRule.Validate(info.Phone);
            if (!phoneResult.IsValid)
            {
                result.AddError("Phone", phoneResult.ErrorMessage!);
            }
        }

        // URL validations
        if (!string.IsNullOrWhiteSpace(info.Website))
        {
            var urlRule = new UrlRule();
            var urlResult = urlRule.Validate(info.Website);
            if (!urlResult.IsValid)
            {
                result.AddError("Website", urlResult.ErrorMessage!);
            }
        }

        if (!string.IsNullOrWhiteSpace(info.LinkedIn))
        {
            var linkedInRule = new LinkedInRule();
            var linkedInResult = linkedInRule.Validate(info.LinkedIn);
            if (!linkedInResult.IsValid)
            {
                result.AddError("LinkedIn", linkedInResult.ErrorMessage!);
            }
        }

        if (!string.IsNullOrWhiteSpace(info.GitHub))
        {
            var gitHubResult = new UrlRule().Validate(info.GitHub);
            if (!gitHubResult.IsValid)
            {
                result.AddError("GitHub", gitHubResult.ErrorMessage!);
            }
        }
    }

    private void ValidateExperience(Experience exp, ResumeValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(exp.JobTitle) && string.IsNullOrWhiteSpace(exp.Company))
        {
            result.AddWarning("Experience", "Experience entry is incomplete");
        }

        if (!exp.IsCurrentRole)
        {
            var dateRange = new DateRangeRule(exp.StartDate, exp.EndDate).Validate(null);
            if (!dateRange.IsValid)
            {
                result.AddError($"Experience: {exp.JobTitle}", "Start date must be before end date");
            }
        }
    }

    private void ValidateEducation(Education edu, ResumeValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(edu.Institution) && string.IsNullOrWhiteSpace(edu.Degree))
        {
            result.AddWarning("Education", "Education entry is incomplete");
        }

        if (!edu.IsCurrentlyStudying)
        {
            var dateRange = new DateRangeRule(edu.StartDate, edu.EndDate).Validate(null);
            if (!dateRange.IsValid)
            {
                result.AddError($"Education: {edu.Degree}", "Start date must be before end date");
            }
        }
    }
}

public class FieldValidator
{
    private readonly List<IValidationRule> _rules = new();

    public FieldValidator Required(string fieldName)
    {
        _rules.Add(new RequiredRule(fieldName));
        return this;
    }

    public FieldValidator Email()
    {
        _rules.Add(new EmailRule());
        return this;
    }

    public FieldValidator Phone()
    {
        _rules.Add(new PhoneRule());
        return this;
    }

    public FieldValidator Url()
    {
        _rules.Add(new UrlRule());
        return this;
    }

    public FieldValidator MinLength(int min, string fieldName)
    {
        _rules.Add(new MinLengthRule(min, fieldName));
        return this;
    }

    public FieldValidator MaxLength(int max, string fieldName)
    {
        _rules.Add(new MaxLengthRule(max, fieldName));
        return this;
    }

    public ValidationResult Validate(object? value)
    {
        foreach (var rule in _rules)
        {
            var result = rule.Validate(value);
            if (!result.IsValid)
                return result;
        }
        return ValidationResult.Success();
    }

    public static FieldValidator Create() => new();
}
