using System.Globalization;

namespace ResumeBuilder.Core.Models;

public enum PaperSize { A4, Letter }
public enum DocumentLanguage { English, Albanian }

/// <summary>Document-only locale, scoped to one render/export operation.</summary>
public static class DocumentText
{
    private static readonly AsyncLocal<DocumentLanguage> Current = new();
    public static CultureInfo Culture => Current.Value == DocumentLanguage.Albanian ? CultureInfo.GetCultureInfo("sq-AL") : CultureInfo.InvariantCulture;
    private static readonly Dictionary<string, string> Albanian = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Professional Summary"] = "P\u00ebrmbledhje profesionale",
        ["Summary"] = "P\u00ebrmbledhje",
        ["Profile"] = "Profili",
        ["Professional Profile"] = "Profili profesional",
        ["About Me"] = "Rreth meje",
        ["About"] = "Rreth meje",
        ["Experience"] = "P\u00ebrvoja",
        ["Work Experience"] = "P\u00ebrvoja e pun\u00ebs",
        ["Professional Experience"] = "P\u00ebrvoja profesionale",
        ["Employment History"] = "Historiku i pun\u00ebsimit",
        ["Work History"] = "Historiku i pun\u00ebs",
        ["Education"] = "Arsimi",
        ["Academic Background"] = "Formimi akademik",
        ["Skills"] = "Aft\u00ebsit\u00eb",
        ["Technical Skills"] = "Aft\u00ebsit\u00eb teknike",
        ["Core Competencies"] = "Kompetencat kryesore",
        ["Expertise"] = "Ekspertiza",
        ["Languages"] = "Gjuh\u00ebt",
        ["Language Skills"] = "Aft\u00ebsit\u00eb gjuh\u00ebsore",
        ["Certifications"] = "Certifikimet",
        ["Certificates"] = "Certifikatat",
        ["Projects"] = "Projektet",
        ["Key Projects"] = "Projektet kryesore",
        ["Selected Projects"] = "Projektet e p\u00ebrzgjedhura",
        ["Contact"] = "Kontakti",
        ["Contact Information"] = "T\u00eb dh\u00ebnat e kontaktit",
        ["Executive Summary"] = "P\u00ebrmbledhje ekzekutive",
        ["Personal statement"] = "Profili personal",
        ["Education and training"] = "Arsimi dhe trajnimi",
        ["Areas of Expertise"] = "Fushat e ekspertiz\u00ebs",
        ["Career Objective"] = "Objektivi profesional",
        ["Certifications and Licenses"] = "Certifikimet dhe licencat",
        ["Language proficiency"] = "Njohurit\u00eb gjuh\u00ebsore",
        ["Language"] = "Gjuha",
        ["Proficiency"] = "Niveli",
        ["Projects & Portfolio"] = "Projektet dhe portofoli",
        ["Research & Professional Experience"] = "P\u00ebrvoja k\u00ebrkimore dhe profesionale",
        ["Research Projects"] = "Projektet k\u00ebrkimore",
        ["Research Statement"] = "Profili k\u00ebrkimor",
        ["Skills and Competencies"] = "Aft\u00ebsit\u00eb dhe kompetencat",
        ["Skills & Abilities"] = "Aft\u00ebsit\u00eb",
        ["Skills and competences"] = "Aft\u00ebsit\u00eb dhe kompetencat",
        ["Duties and Responsibilities"] = "Detyrat dhe p\u00ebrgjegj\u00ebsit\u00eb",
        ["Key Accomplishments"] = "Arritjet kryesore",
        ["Present"] = "Sot",
        ["Beginner"] = "Fillestar",
        ["Elementary"] = "Elementar",
        ["Intermediate"] = "Mesatar",
        ["Advanced"] = "I p\u00ebrparuar",
        ["Expert"] = "Ekspert",
        ["Basic"] = "Baz\u00eb",
        ["Conversational"] = "Bisedor",
        ["Professional"] = "Profesional",
        ["Fluent"] = "Rrjedhsh\u00ebm",
        ["Native"] = "Amtar"
    };
    public static string Translate(string text)
    {
        if (Current.Value != DocumentLanguage.Albanian || !Albanian.TryGetValue(text, out var translated)) return text;
        return text == text.ToUpperInvariant() ? translated.ToUpperInvariant() : translated;
    }
    public static IDisposable Use(DocumentLanguage language)
    {
        var previous = Current.Value;
        Current.Value = language;
        return new Scope(previous);
    }
    private sealed class Scope(DocumentLanguage previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
