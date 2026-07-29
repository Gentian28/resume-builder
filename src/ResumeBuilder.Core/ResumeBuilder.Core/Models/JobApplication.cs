namespace ResumeBuilder.Core.Models;

/// <summary>Where an application has got to.</summary>
public enum ApplicationStatus
{
    /// <summary>Found and worth applying to, but not sent.</summary>
    Saved,

    /// <summary>Sent. The default for a newly tracked application.</summary>
    Applied,

    /// <summary>They replied and something is scheduled or in progress.</summary>
    Interviewing,

    Offer,

    Rejected,

    /// <summary>No reply, long enough that it is over. Distinguished from rejected because it is
    /// the most common outcome and reads very differently in a list.</summary>
    NoResponse
}

/// <summary>
/// One job application: which résumé went where, when, and what happened.
///
/// The résumés already carry <see cref="Resume.TargetRole"/> and <see cref="Resume.JobDescription"/>
/// once tailored, so the work of adapting a CV per application is stored — but a list of variants
/// with the same role name and no company, date, or outcome cannot answer "which version did this
/// company read?". That question is the point of this entity: when the interview call comes, the
/// answer has to be one click away.
/// </summary>
public class JobApplication
{
    public int Id { get; set; }

    /// <summary>
    /// The résumé that was actually sent — usually a tailored variant. Nullable because someone
    /// may track an application before deciding which version to send, and because deleting a
    /// résumé must not delete the record that they applied.
    /// </summary>
    public int? ResumeId { get; set; }

    public string Company { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

    /// <summary>
    /// When it was sent. Null while the status is <see cref="ApplicationStatus.Saved"/> — a job
    /// you have not applied to has no application date, and inventing one makes "how long have
    /// they had this?" wrong.
    /// </summary>
    public DateTime? AppliedOn { get; set; }

    /// <summary>The posting, so it can be reopened. Not validated — a pasted link is better than
    /// a rejected one.</summary>
    public string Link { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whole days since it was sent, or null if it has not been. Drives the "silent for three
    /// weeks" reading that makes the list worth opening, so it is computed rather than stored —
    /// a stored value is wrong the next morning.
    /// </summary>
    public int? DaysSinceApplied => AppliedOn is null
        ? null
        : Math.Max(0, (int)(DateTime.UtcNow.Date - AppliedOn.Value.Date).TotalDays);

    /// <summary>
    /// Whether this is waiting on the other side and has been for a while. Deliberately not a
    /// stored flag: "needs chasing" is a question about today, not a property of the record.
    /// </summary>
    public bool IsStale(int afterDays = 14) =>
        Status == ApplicationStatus.Applied && DaysSinceApplied >= afterDays;
}
