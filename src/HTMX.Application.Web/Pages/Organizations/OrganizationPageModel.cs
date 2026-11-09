namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// What every tab of an organization's page shares: the organization the route names, and its header with Archive or
/// Restore. The identifier, the toast and the answers an action gives are <see cref="RecordPageModel"/>'s.
/// </summary>
public abstract class OrganizationPageModel : RecordPageModel
{
    private const string ArchiveHandler = "Archive";
    private const string RestoreHandler = "Restore";

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationPageModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads and changes the organization.</param>
    protected OrganizationPageModel(IOrganizationService organizations)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        this.Organizations = organizations;
    }

    /// <summary>
    /// Gets the organization the page shows, or <see langword="null"/> when the identifier names none.
    /// </summary>
    public OrganizationSingleResponse? Organization { get; private set; }

    /// <summary>
    /// Gets the service the tab reads and changes the organization through.
    /// </summary>
    protected IOrganizationService Organizations { get; }

    /// <inheritdoc/>
    protected override string ContentTarget => "#organization";

    /// <summary>
    /// Archives the organization, and says so in a toast whose Undo restores it.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostArchiveAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Archived, cancellationToken);
    }

    /// <summary>
    /// Restores the organization, and says so in a toast whose Undo archives it again.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    public Task<IActionResult> OnPostRestoreAsync(CancellationToken cancellationToken)
    {
        return this.ChangeStatusAsync(ChangeKind.Restored, cancellationToken);
    }

    /// <inheritdoc/>
    protected override async Task<bool> FindRecordAsync(CancellationToken cancellationToken)
    {
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        return organization is not null;
    }

    /// <summary>
    /// Reads, after the organization, whatever else the tab shows of it. The Details tab needs nothing more.
    /// </summary>
    /// <param name="id">The organization's identifier.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when it is read.</returns>
    protected virtual Task ReadMoreAsync(OrganizationId id, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads the organization the route names and, when there is one, what the tab shows of it.
    /// </summary>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The organization, or <see langword="null"/> when there is none.</returns>
    protected async Task<OrganizationSingleResponse?> ReadAsync(CancellationToken cancellationToken)
    {
        OrganizationId id = new OrganizationId(this.Input.Id);
        this.Organization = await this.Organizations.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (this.Organization is not null)
        {
            await this.ReadMoreAsync(id, cancellationToken).ConfigureAwait(false);
        }

        return this.Organization;
    }

    /// <summary>
    /// Goes to the organization's own page after an action found it gone, so that the page says so.
    /// </summary>
    /// <returns>A redirect, or 204 with the request to load the page.</returns>
    protected IActionResult ShowGone()
    {
        return HtmxAnswers.NextPage(this, "./Details", new { id = this.Input.Id });
    }

    /// <summary>
    /// Writes the toast's message for what became of an archive or a restore.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="outcome">What became of it.</param>
    /// <param name="name">The organization's name.</param>
    /// <returns>The message.</returns>
    private static string StatusMessage(ChangeKind change, UpdateOutcome outcome, OrganizationName name)
    {
        bool archiving = change == ChangeKind.Archived;
        return outcome switch
        {
            UpdateOutcome.Saved =>
                archiving ? $"{name.Value} archived." : $"{name.Value} restored.",
            UpdateOutcome.Unchanged =>
                archiving ? $"{name.Value} was already archived." : $"{name.Value} was already active.",
            _ =>
                $"{name.Value} was changed by someone else at the same moment. Try again.",
        };
    }

    /// <summary>
    /// Archives or restores the organization, reads it again, and answers with what it now is and a toast that says
    /// what happened. When the organization no longer exists, its page says so.
    /// </summary>
    /// <param name="change">Archived or Restored.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise.</returns>
    private async Task<IActionResult> ChangeStatusAsync(ChangeKind change, CancellationToken cancellationToken)
    {
        OrganizationStatusRequest request = new OrganizationStatusRequest
        {
            Id = new OrganizationId(this.Input.Id),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = change == ChangeKind.Archived
            ? await this.Organizations.ArchiveAsync(request, cancellationToken).ConfigureAwait(false)
            : await this.Organizations.RestoreAsync(request, cancellationToken).ConfigureAwait(false);
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return this.ShowGone();
        }

        // Undo is offered only for a change that was made: it posts the opposite change.
        string? undo = null;
        if (outcome == UpdateOutcome.Saved)
        {
            string opposite = change == ChangeKind.Archived ? RestoreHandler : ArchiveHandler;
            undo = this.HandlerAddress(opposite, this.TabValues());
        }

        return this.Answer(StatusMessage(change, outcome, organization.Name), undo);
    }
}
