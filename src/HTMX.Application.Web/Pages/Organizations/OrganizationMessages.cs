namespace HTMX.Application.Web.Pages.Organizations;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The toast's words for what became of archiving, restoring or deleting an organization, the same on the
/// organization's own page as on the organizations list.
/// </summary>
public static class OrganizationMessages
{
    /// <summary>
    /// Writes the toast's message for what became of an archive or a restore.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="outcome">What became of it.</param>
    /// <param name="name">The organization's name, or words that stand for it when it is not known.</param>
    /// <returns>The message.</returns>
    public static string Status(ChangeKind change, UpdateOutcome outcome, string name)
    {
        bool archiving = change == ChangeKind.Archived;
        return outcome switch
        {
            UpdateOutcome.Saved =>
                archiving ? $"{name} archived." : $"{name} restored.",
            UpdateOutcome.Unchanged =>
                archiving ? $"{name} was already archived." : $"{name} was already active.",
            UpdateOutcome.NotFound =>
                "That organization no longer exists.",
            _ =>
                $"{name} was changed by someone else at the same moment. Try again.",
        };
    }

    /// <summary>
    /// Writes the toast's message for a delete that was refused.
    /// </summary>
    /// <param name="outcome">Why it was refused.</param>
    /// <param name="name">The organization's name.</param>
    /// <returns>The message.</returns>
    public static string Delete(DeleteOutcome outcome, OrganizationName name)
    {
        return outcome switch
        {
            DeleteOutcome.Active =>
                $"{name.Value} is active again, so it can't be deleted.",
            DeleteOutcome.InUse =>
                $"Contacts now belong to {name.Value}, so it can't be deleted.",
            _ =>
                $"{name.Value} has changed since the page was shown, so it wasn't deleted. Check it, then try again.",
        };
    }
}
