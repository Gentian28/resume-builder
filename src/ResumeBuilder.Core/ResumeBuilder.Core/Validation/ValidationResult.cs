namespace ResumeBuilder.Core.Validation;

public class ValidationResult
{
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    private ValidationResult(bool isValid, string? errorMessage = null)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

public class FieldValidationResult
{
    public string FieldName { get; }
    public bool IsValid { get; }
    public string? ErrorMessage { get; }

    public FieldValidationResult(string fieldName, bool isValid, string? errorMessage = null)
    {
        FieldName = fieldName;
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static FieldValidationResult Success(string fieldName) => new(fieldName, true);
    public static FieldValidationResult Failure(string fieldName, string errorMessage) => new(fieldName, false, errorMessage);
}

public class ResumeValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<FieldValidationResult> Errors { get; } = new();
    public List<FieldValidationResult> Warnings { get; } = new();

    public void AddError(string fieldName, string message)
    {
        Errors.Add(FieldValidationResult.Failure(fieldName, message));
    }

    public void AddWarning(string fieldName, string message)
    {
        Warnings.Add(new FieldValidationResult(fieldName, true, message));
    }
}
