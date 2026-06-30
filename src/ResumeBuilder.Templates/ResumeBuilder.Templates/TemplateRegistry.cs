using ResumeBuilder.Core.Models;
using ResumeBuilder.Templates.Templates;

namespace ResumeBuilder.Templates;

public class TemplateRegistry
{
    private readonly Dictionary<string, IResumeTemplate> _templates;

    public TemplateRegistry()
    {
        _templates = new Dictionary<string, IResumeTemplate>(StringComparer.OrdinalIgnoreCase);
        RegisterDefaultTemplates();
    }

    private void RegisterDefaultTemplates()
    {
        Register(new ModernTemplate());
        Register(new ClassicTemplate());
        Register(new MinimalTemplate());
        Register(new CreativeTemplate());
        Register(new ExecutiveTemplate());
        Register(new TechnicalTemplate());
        Register(new AcademicTemplate());
        Register(new TwoColumnTemplate());
        Register(new CompactTemplate());
        Register(new ElegantTemplate());
        Register(new ProfessionalTemplate());
        Register(new StarterTemplate());
        Register(new SimpleTemplate());
        Register(new TimelineTemplate());
        Register(new BoldTemplate());
        Register(new DarkSidebarTemplate());
        Register(new InfographicTemplate());
    }

    public void Register(IResumeTemplate template)
    {
        _templates[template.Info.Id] = template;
    }

    public IResumeTemplate? GetTemplate(string id)
    {
        return _templates.TryGetValue(id, out var template) ? template : null;
    }

    public IResumeTemplate GetTemplateOrDefault(string id)
    {
        return GetTemplate(id) ?? _templates["modern"];
    }

    public IEnumerable<IResumeTemplate> GetAllTemplates()
    {
        return _templates.Values;
    }

    public IEnumerable<TemplateInfo> GetAllTemplateInfos()
    {
        return _templates.Values.Select(t => t.Info);
    }

    public IEnumerable<IResumeTemplate> GetTemplatesByCategory(TemplateCategory category)
    {
        return _templates.Values.Where(t => t.Info.Category == category);
    }

    public IEnumerable<IResumeTemplate> GetTemplatesByLayout(TemplateLayout layout)
    {
        return _templates.Values.Where(t => t.Info.Layout == layout);
    }

    public IEnumerable<IResumeTemplate> SearchTemplates(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAllTemplates();

        var lowerQuery = query.ToLower();

        return _templates.Values.Where(t =>
            t.Info.Name.ToLower().Contains(lowerQuery) ||
            t.Info.Description.ToLower().Contains(lowerQuery) ||
            t.Info.Tags.Any(tag => tag.ToLower().Contains(lowerQuery)));
    }
}
