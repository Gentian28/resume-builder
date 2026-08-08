namespace ResumeBuilder.Core.SpellCheck;

/// <summary>
/// Words the stock Hunspell dictionaries don't know but a résumé routinely contains. Without
/// this list the checker flags ordinary industry vocabulary and then offers its nearest
/// dictionary neighbour as a "fix" (Kubernetes → "Rubbernecks"), which reads as a broken
/// feature. Ships with the app — unlike the personal dictionary, every install gets it and
/// updates extend it.
/// </summary>
public static class BuiltInVocabulary
{
    public static bool Contains(string word) => Words.Contains(word);

    /// <summary>
    /// Case-insensitive on purpose: users write "kubernetes" and "Kubernetes" interchangeably,
    /// and casing correctness is not this feature's job.
    /// </summary>
    private static readonly HashSet<string> Words = new(new[]
    {
        // Job activities and résumé phrasing
        "architecting", "backlog", "codebase", "codebases", "deliverables", "downtime",
        "failover", "monorepo", "monorepos", "offboarding", "onboarding", "performant",
        "prioritization", "refactor", "refactored", "refactoring", "refactors", "reskilling",
        "roadmap", "roadmaps", "scalable", "scalability", "standups", "uptime", "upskilling",
        "workflows",

        // Methodology
        "agile", "kanban", "scrum", "sprints", "retrospectives", "DevOps", "DevSecOps",
        "MLOps", "GitOps",

        // Acronym plurals (the all-caps skip only covers fully uppercase words)
        "APIs", "CDNs", "CPUs", "CRMs", "CVs", "ERPs", "ETLs", "GPUs", "IDEs", "KPIs",
        "LLMs", "MVPs", "OKRs", "PDFs", "POCs", "SDKs", "SLAs", "SLOs", "SPAs", "UIs",
        "URLs", "VMs", "VPNs",

        // Architecture and infrastructure
        "autoscaling", "containerization", "containerized", "microservice", "microservices",
        "middleware", "multitenant", "observability", "serverless", "SaaS", "PaaS", "IaaS",
        "NoSQL", "webhook", "webhooks", "WebSocket", "WebSockets", "WebAuthn",

        // Platforms and tools
        "Ansible", "Auth0", "Azure", "Bitbucket", "CircleCI", "Cloudflare", "Confluence",
        "Databricks", "Datadog", "Docker", "Dockerfile", "Elasticsearch", "Figma", "Firebase",
        "GitHub", "GitLab", "Grafana", "Heroku", "Jenkins", "Jira", "Kibana", "Kubernetes",
        "Logstash", "Netlify", "Nginx", "Okta", "Prometheus", "RabbitMQ", "Redis", "Redshift",
        "Snowflake", "Splunk", "Terraform", "Trello", "Vercel",

        // Databases
        "BigQuery", "Cassandra", "DynamoDB", "MariaDB", "MongoDB", "MSSQL", "MySQL",
        "Postgres", "PostgreSQL", "SQLite",

        // Languages, frameworks, libraries
        "Avalonia", "Blazor", "Bootstrap", "csharp", "Django", "dotnet", "ESLint", "Elixir",
        "FastAPI", "GraphQL", "gRPC", "Golang", "jQuery", "JavaScript", "Jupyter", "Keras",
        "Kotlin", "Laravel", "Micronaut", "Nuxt", "NumPy", "NuGet", "Nodejs", "npm", "OAuth",
        "OAuth2", "Pandas", "pnpm", "PyTorch", "Quarkus", "RESTful", "Redux", "Rollup",
        "scikit", "sklearn", "Svelte", "Symfony", "Tailwind", "TensorFlow", "TypeScript",
        "Vite", "Vue", "webpack", "WinForms", "Xamarin", "Xcode",

        // AI
        "agentic", "Anthropic", "ChatGPT", "chatbot", "chatbots", "Claude", "embeddings",
        "GPT", "OpenAI",

        // Operating systems and devices
        "CentOS", "Debian", "iOS", "iPadOS", "macOS", "Ubuntu",
    }, StringComparer.OrdinalIgnoreCase);
}
