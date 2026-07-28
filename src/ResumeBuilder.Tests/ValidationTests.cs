using AwesomeAssertions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.Validation;

namespace ResumeBuilder.Tests;

public class ValidationTests
{
    private readonly ResumeValidator _validator = new();

    [Fact]
    public void Validate_EmptyResume_ReturnsErrors()
    {
        // Arrange
        var resume = new Resume();

        // Act
        var result = _validator.Validate(resume);

        // Assert
        // Empty resume without name returns error
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ValidResume_ReturnsNoErrors()
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com"
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "",
                LastName = "",
                Email = "john@example.com"
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.FieldName == "Name");
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsError()
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "invalid-email"
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.FieldName == "Email");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.org")]
    public void Validate_ValidEmail_NoError(string email)
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = email
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.Errors.Should().NotContain(e => e.FieldName == "Email");
    }

    [Theory]
    [InlineData("555-1234")]
    [InlineData("+1-555-123-4567")]
    [InlineData("(555) 123-4567")]
    [InlineData("+44 20 7946 0958")]
    public void Validate_ValidPhone_NoError(string phone)
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = phone
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.Errors.Should().NotContain(e => e.FieldName == "Phone");
    }

    [Fact]
    public void Validate_InvalidUrl_ReturnsError()
    {
        // Arrange - use truly invalid URL with illegal characters
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Website = "http://example[invalid].com"
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.FieldName == "Website");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://www.example.com/path")]
    [InlineData("https://github.com/user")]
    public void Validate_ValidUrl_NoError(string url)
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Website = url
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.Errors.Should().NotContain(e => e.FieldName == "Website");
    }

    [Fact]
    public void Validate_ExperienceEndDateBeforeStartDate_ReturnsError()
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            Experiences = new List<Experience>
            {
                new()
                {
                    JobTitle = "Developer",
                    Company = "Tech Corp",
                    StartDate = new DateTime(2023, 1, 1),
                    EndDate = new DateTime(2022, 1, 1) // Before start date
                }
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage != null && e.ErrorMessage.Contains("date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_EducationEndDateBeforeStartDate_ReturnsError()
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            EducationList = new List<Education>
            {
                new()
                {
                    Degree = "BSc",
                    Institution = "University",
                    StartDate = new DateTime(2020, 1, 1),
                    EndDate = new DateTime(2019, 1, 1) // Before start date
                }
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage != null && e.ErrorMessage.Contains("date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_CurrentRole_NoEndDateError()
    {
        // Arrange
        var resume = new Resume
        {
            PersonalInfo = new PersonalInfo
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            Experiences = new List<Experience>
            {
                new()
                {
                    JobTitle = "Developer",
                    Company = "Tech Corp",
                    StartDate = new DateTime(2023, 1, 1),
                    EndDate = null,
                    IsCurrentRole = true
                }
            }
        };

        // Act
        var result = _validator.Validate(resume);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

public class ResumeValidationResultTests
{
    [Fact]
    public void ValidationResult_NoErrors_IsValid()
    {
        // Arrange
        var result = new ResumeValidationResult();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationResult_WithErrors_IsNotValid()
    {
        // Arrange
        var result = new ResumeValidationResult();
        result.AddError("Field", "Error message");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void ValidationResult_AddError_AddsCorrectly()
    {
        // Arrange
        var result = new ResumeValidationResult();

        // Act
        result.AddError("TestField", "Test message");

        // Assert
        var error = result.Errors.First();
        error.FieldName.Should().Be("TestField");
        error.ErrorMessage.Should().Be("Test message");
    }
}
