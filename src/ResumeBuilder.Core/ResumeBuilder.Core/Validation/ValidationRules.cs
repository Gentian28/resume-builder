using System.Text.RegularExpressions;

namespace ResumeBuilder.Core.Validation;

public class RequiredRule : ValidationRule<string>
{
    private readonly string _fieldName;

    public RequiredRule(string fieldName)
    {
        _fieldName = fieldName;
    }

    public override ValidationResult Validate(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ValidationResult.Failure($"{_fieldName} is required")
            : ValidationResult.Success();
    }
}

public class EmailRule : ValidationRule<string>
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success(); // Not required, use RequiredRule for that

        return EmailRegex.IsMatch(value)
            ? ValidationResult.Success()
            : ValidationResult.Failure("Invalid email format");
    }
}

public class PhoneRule : ValidationRule<string>
{
    private static readonly Regex PhoneRegex = new(
        @"^[\d\s\-\+\(\)\.]+$",
        RegexOptions.Compiled);

    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success();

        if (value.Length < 7)
            return ValidationResult.Failure("Phone number is too short");

        return PhoneRegex.IsMatch(value)
            ? ValidationResult.Success()
            : ValidationResult.Failure("Invalid phone format");
    }
}

public class UrlRule : ValidationRule<string>
{
    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success();

        // Allow URLs without protocol
        var urlToCheck = value;
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            urlToCheck = "https://" + value;
        }

        return Uri.TryCreate(urlToCheck, UriKind.Absolute, out _)
            ? ValidationResult.Success()
            : ValidationResult.Failure("Invalid URL format");
    }
}

public class LinkedInRule : ValidationRule<string>
{
    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success();

        // Accept full URL or just the profile part
        if (value.Contains("linkedin.com", StringComparison.OrdinalIgnoreCase))
        {
            return new UrlRule().Validate(value);
        }

        // Just a username/profile ID
        return ValidationResult.Success();
    }
}

public class MinLengthRule : ValidationRule<string>
{
    private readonly int _minLength;
    private readonly string _fieldName;

    public MinLengthRule(int minLength, string fieldName)
    {
        _minLength = minLength;
        _fieldName = fieldName;
    }

    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success();

        return value.Length >= _minLength
            ? ValidationResult.Success()
            : ValidationResult.Failure($"{_fieldName} must be at least {_minLength} characters");
    }
}

public class MaxLengthRule : ValidationRule<string>
{
    private readonly int _maxLength;
    private readonly string _fieldName;

    public MaxLengthRule(int maxLength, string fieldName)
    {
        _maxLength = maxLength;
        _fieldName = fieldName;
    }

    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success();

        return value.Length <= _maxLength
            ? ValidationResult.Success()
            : ValidationResult.Failure($"{_fieldName} cannot exceed {_maxLength} characters");
    }
}

public class DateRangeRule : IValidationRule
{
    private readonly DateTime? _startDate;
    private readonly DateTime? _endDate;

    public DateRangeRule(DateTime? startDate, DateTime? endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public ValidationResult Validate(object? value)
    {
        if (_startDate == null || _endDate == null)
            return ValidationResult.Success();

        return _startDate <= _endDate
            ? ValidationResult.Success()
            : ValidationResult.Failure("Start date must be before end date");
    }
}
