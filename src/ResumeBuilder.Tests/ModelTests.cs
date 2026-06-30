using FluentAssertions;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Tests;

public class ResumeModelTests
{
    [Fact]
    public void Resume_DefaultConstructor_InitializesCollections()
    {
        // Act
        var resume = new Resume();

        // Assert
        resume.PersonalInfo.Should().NotBeNull();
        resume.Experiences.Should().NotBeNull();
        resume.EducationList.Should().NotBeNull();
        resume.Skills.Should().NotBeNull();
        resume.Languages.Should().NotBeNull();
        resume.Certifications.Should().NotBeNull();
        resume.Projects.Should().NotBeNull();
    }

    [Fact]
    public void Resume_Name_DefaultsToUntitled()
    {
        // Act
        var resume = new Resume();

        // Assert
        resume.Name.Should().Be("Untitled Resume");
    }

    [Fact]
    public void PersonalInfo_FullName_CombinesFirstAndLast()
    {
        // Arrange
        var info = new PersonalInfo
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act & Assert
        info.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void PersonalInfo_Location_CombinesCityAndCountry()
    {
        // Arrange
        var info = new PersonalInfo
        {
            City = "New York",
            Country = "USA"
        };

        // Act & Assert
        info.Location.Should().Be("New York, USA");
    }

    [Fact]
    public void PersonalInfo_Location_OnlyCity()
    {
        // Arrange
        var info = new PersonalInfo
        {
            City = "New York",
            Country = ""
        };

        // Act & Assert
        info.Location.Should().Be("New York");
    }

    [Fact]
    public void Experience_Order_DefaultsToZero()
    {
        // Act
        var exp = new Experience();

        // Assert
        exp.Order.Should().Be(0);
    }

    [Fact]
    public void Experience_Achievements_InitializesAsEmptyList()
    {
        // Act
        var exp = new Experience();

        // Assert
        exp.Achievements.Should().NotBeNull();
        exp.Achievements.Should().BeEmpty();
    }

    [Fact]
    public void Experience_DateRange_FormatsCorrectly()
    {
        // Arrange
        var exp = new Experience
        {
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2023, 6, 15)
        };

        // Assert
        exp.DateRange.Should().Be("Jan 2020 - Jun 2023");
    }

    [Fact]
    public void Experience_DateRange_CurrentRole()
    {
        // Arrange
        var exp = new Experience
        {
            StartDate = new DateTime(2020, 1, 1),
            IsCurrentRole = true
        };

        // Assert
        exp.DateRange.Should().Be("Jan 2020 - Present");
    }

    [Fact]
    public void Skill_Level_DefaultsToIntermediate()
    {
        // Act
        var skill = new Skill();

        // Assert
        skill.Level.Should().Be(SkillLevel.Intermediate);
    }

    [Fact]
    public void TemplateSettings_Defaults_AreReasonable()
    {
        // Act
        var settings = new TemplateSettings();

        // Assert
        settings.AccentColor.Should().NotBeNullOrEmpty();
        settings.FontFamily.Should().NotBeNullOrEmpty();
        settings.FontSizeScale.Should().BeGreaterThan(0);
        settings.LineSpacing.Should().BeGreaterThan(0);
        settings.PageMargin.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SectionOrder_Defaults_ContainsAllSections()
    {
        // Act
        var order = new SectionOrder();

        // Assert
        order.OrderedSections.Should().NotBeEmpty();
        order.OrderedSections.Should().Contain(SectionType.Experience);
        order.OrderedSections.Should().Contain(SectionType.Education);
        order.OrderedSections.Should().Contain(SectionType.Skills);
    }

    [Fact]
    public void SectionOrder_Visibility_DefaultsToAllVisible()
    {
        // Act
        var order = new SectionOrder();

        // Assert
        order.Visibility.Values.Should().AllBeEquivalentTo(true);
    }

    [Fact]
    public void Language_Proficiency_CanBeSet()
    {
        // Arrange
        var lang = new Language
        {
            Name = "English",
            Proficiency = LanguageProficiency.Native
        };

        // Assert
        lang.Proficiency.Should().Be(LanguageProficiency.Native);
    }

    [Fact]
    public void Certification_Properties_CanBeSet()
    {
        // Arrange
        var cert = new Certification
        {
            Name = "AWS Certified Developer",
            IssuingOrganization = "Amazon Web Services",
            IssueDate = new DateTime(2023, 1, 1),
            ExpirationDate = new DateTime(2026, 1, 1),
            CredentialId = "ABC123"
        };

        // Assert
        cert.Name.Should().Be("AWS Certified Developer");
        cert.IssuingOrganization.Should().Be("Amazon Web Services");
        cert.CredentialId.Should().Be("ABC123");
    }

    [Fact]
    public void Project_Technologies_CanContainMultiple()
    {
        // Arrange
        var project = new Project
        {
            Name = "Resume Builder",
            Description = "A desktop app for building resumes",
            Technologies = new List<string> { "C#", "Avalonia", "QuestPDF" }
        };

        // Assert
        project.Technologies.Should().Contain("C#");
        project.Technologies.Should().Contain("Avalonia");
        project.Technologies.Should().HaveCount(3);
    }

    [Fact]
    public void Education_DegreeWithField_CombinesCorrectly()
    {
        // Arrange
        var edu = new Education
        {
            Degree = "Bachelor of Science",
            FieldOfStudy = "Computer Science"
        };

        // Assert
        edu.DegreeWithField.Should().Be("Bachelor of Science in Computer Science");
    }

    [Fact]
    public void Education_DegreeWithField_OnlyDegree()
    {
        // Arrange
        var edu = new Education
        {
            Degree = "PhD",
            FieldOfStudy = ""
        };

        // Assert
        edu.DegreeWithField.Should().Be("PhD");
    }
}

public class SkillLevelTests
{
    [Theory]
    [InlineData(SkillLevel.Beginner)]
    [InlineData(SkillLevel.Intermediate)]
    [InlineData(SkillLevel.Advanced)]
    [InlineData(SkillLevel.Expert)]
    public void SkillLevel_AllLevels_AreDefined(SkillLevel level)
    {
        // Assert
        Enum.IsDefined(typeof(SkillLevel), level).Should().BeTrue();
    }
}

public class LanguageProficiencyTests
{
    [Theory]
    [InlineData(LanguageProficiency.Basic)]
    [InlineData(LanguageProficiency.Conversational)]
    [InlineData(LanguageProficiency.Professional)]
    [InlineData(LanguageProficiency.Fluent)]
    [InlineData(LanguageProficiency.Native)]
    public void LanguageProficiency_AllLevels_AreDefined(LanguageProficiency level)
    {
        // Assert
        Enum.IsDefined(typeof(LanguageProficiency), level).Should().BeTrue();
    }
}

public class SectionTypeTests
{
    [Fact]
    public void SectionType_ContainsExpectedSections()
    {
        // Assert
        Enum.GetValues<SectionType>().Should().Contain(SectionType.PersonalInfo);
        Enum.GetValues<SectionType>().Should().Contain(SectionType.Summary);
        Enum.GetValues<SectionType>().Should().Contain(SectionType.Experience);
        Enum.GetValues<SectionType>().Should().Contain(SectionType.Education);
        Enum.GetValues<SectionType>().Should().Contain(SectionType.Skills);
        Enum.GetValues<SectionType>().Should().Contain(SectionType.Projects);
        Enum.GetValues<SectionType>().Should().Contain(SectionType.Certifications);
        Enum.GetValues<SectionType>().Should().Contain(SectionType.Languages);
    }
}
