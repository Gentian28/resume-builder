namespace ResumeBuilder.Core.Validation;

public interface IValidationRule
{
    ValidationResult Validate(object? value);
}

public interface IValidationRule<T> : IValidationRule
{
    ValidationResult Validate(T? value);
}

public abstract class ValidationRule<T> : IValidationRule<T>
{
    public abstract ValidationResult Validate(T? value);

    ValidationResult IValidationRule.Validate(object? value)
    {
        if (value is T typedValue)
            return Validate(typedValue);
        if (value == null)
            return Validate(default);
        return ValidationResult.Failure($"Expected type {typeof(T).Name} but got {value.GetType().Name}");
    }
}
