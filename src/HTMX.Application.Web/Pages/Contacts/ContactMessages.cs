namespace HTMX.Application.Web.Pages.Contacts;

using HTMX.Adapter.Customers.Contracts;

/// <summary>
/// The toast's words for what became of archiving or restoring a contact, the same on the contact's own page as on
/// their organization's Contacts tab.
/// </summary>
public static class ContactMessages
{
    /// <summary>
    /// Writes the toast's message for what became of an archive or a restore.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="outcome">What became of it.</param>
    /// <param name="name">The contact's name, or words that stand for it when it is not known.</param>
    /// <param name="organization">The name of the organization the request was made from.</param>
    /// <returns>The message.</returns>
    public static string Status(ChangeKind change, UpdateOutcome outcome, string name, OrganizationName organization)
    {
        bool archiving = change == ChangeKind.Archived;
        return outcome switch
        {
            UpdateOutcome.Saved =>
                archiving ? $"{name} archived." : $"{name} restored.",
            UpdateOutcome.Unchanged =>
                archiving ? $"{name} was already archived." : $"{name} was already active.",
            UpdateOutcome.Archived =>
                $"{organization.Value} is archived, so its contacts can't be changed until it is restored.",
            UpdateOutcome.NotFound =>
                $"That contact no longer belongs to {organization.Value}.",
            _ =>
                $"{name} was changed by someone else at the same moment. Try again.",
        };
    }
}
