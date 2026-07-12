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
    private const int MinDigits = 7;
    private const int MaxDigits = 15; // E.164 upper bound

    private static readonly Regex AllowedCharsRegex = new(
        @"^[\d\s\-\+\(\)\.]+$",
        RegexOptions.Compiled);

    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success();

        if (!AllowedCharsRegex.IsMatch(value))
            return ValidationResult.Failure("Invalid phone format");

        // Count digits, not characters: "-------" is seven characters but no phone number, and
        // "+1 (2) 345-6789" is well past seven digits despite the punctuation.
        var digits = value.Count(char.IsDigit);

        if (digits < MinDigits)
            return ValidationResult.Failure("Phone number is too short");

        if (digits > MaxDigits)
            return ValidationResult.Failure("Phone number is too long");

        return ValidationResult.Success();
    }
}

public class UrlRule : ValidationRule<string>
{
    public override ValidationResult Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ValidationResult.Success();

        if (value.Any(char.IsWhiteSpace))
            return ValidationResult.Failure("Invalid URL format");

        var hasScheme =
            value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        // Reject other schemes outright. Without this, "javascript:alert(1)" parses as a valid
        // absolute Uri and later becomes a live href in the HTML export.
        if (!hasScheme && value.Contains(':') && !value.Contains("://"))
            return ValidationResult.Failure("Only http and https URLs are supported");

        var urlToCheck = hasScheme ? value : "https://" + value;

        if (!Uri.TryCreate(urlToCheck, UriKind.Absolute, out var uri))
            return ValidationResult.Failure("Invalid URL format");

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return ValidationResult.Failure("Only http and https URLs are supported");

        // A bare word like "developer" would otherwise become the "valid" URL https://developer.
        if (!uri.Host.Contains('.'))
            return ValidationResult.Failure("Invalid URL format");

        return ValidationResult.Success();
    }

    /// <summary>
    /// Returns a safe absolute http(s) URL for the value, or null if it isn't one. Exporters use
    /// this so a stored "javascript:" value can never become a clickable link.
    /// </summary>
    public static string? ToSafeAbsoluteUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (new UrlRule().Validate(value) is { IsValid: false })
            return null;

        var hasScheme =
            value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

        return hasScheme ? value : "https://" + value;
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
