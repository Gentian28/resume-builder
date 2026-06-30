using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.Validation;

public class ResumeValidator
{
    public ResumeValidationResult Validate(Resume resume)
    {
        var result = new ResumeValidationResult();

        // Personal Info validation
        ValidatePersonalInfo(resume.PersonalInfo, result);

        // Summary validation
        if (!string.IsNullOrWhiteSpace(resume.Summary) && resume.Summary.Length > 5000)
        {
            result.AddError("Summary", "Summary cannot exceed 5000 characters");
        }

        // Experience validation
        foreach (var exp in resume.Experiences)
        {
            ValidateExperience(exp, result);
        }

        // Education validation
        foreach (var edu in resume.EducationList)
        {
            ValidateEducation(edu, result);
        }

        return result;
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
    }

    private void ValidateExperience(Experience exp, ResumeValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(exp.JobTitle) && string.IsNullOrWhiteSpace(exp.Company))
        {
            result.AddWarning("Experience", "Experience entry is incomplete");
        }

        if (exp.StartDate.HasValue && exp.EndDate.HasValue && !exp.IsCurrentRole)
        {
            if (exp.StartDate > exp.EndDate)
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

        if (edu.StartDate.HasValue && edu.EndDate.HasValue && !edu.IsCurrentlyStudying)
        {
            if (edu.StartDate > edu.EndDate)
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
