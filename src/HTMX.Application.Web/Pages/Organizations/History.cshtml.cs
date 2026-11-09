namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// The History tab of an organization's page, at /organizations/{id}/history: every version, newest first, with what
/// each changed, and a roll-back to any earlier one. Rolling back saves the earlier values as a new version, so it says
/// so in a toast whose Undo rolls back again, to the version it replaced.
/// </summary>
public sealed class HistoryModel : OrganizationPageModel
{
    private const string RollBackHandler = "RollBack";

    /// <summary>
    /// Initializes a new instance of the <see cref="HistoryModel"/> class.
    /// </summary>
    /// <param name="organizations">Reads the organization and its versions, and rolls it back.</param>
    public HistoryModel(IOrganizationService organizations)
        : base(organizations)
    {
    }

    /// <inheritdoc/>
    public override string PageName => "./History";

    /// <inheritdoc/>
    public override string ContentPartial => "_OrganizationHistory";

    /// <summary>
    /// Gets the organization's versions, newest first.
    /// </summary>
    public IReadOnlyList<OrganizationVersionResponse> Versions { get; private set; } = [];

    /// <summary>
    /// Gets the versions as the timeline shows them, or <see langword="null"/> until they are read.
    /// </summary>
    public Timeline? Timeline { get; private set; }

    /// <summary>
    /// Rolls the organization back to an earlier version, from the version the page showed it at.
    /// </summary>
    /// <param name="to">The version to roll back to, from the preview's form.</param>
    /// <param name="version">The version the page showed the organization at, from the preview's form.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The tab's content and the toast for HTMX; a redirect back to the tab otherwise; 400 for a version
    /// number that cannot be one.</returns>
    public async Task<IActionResult> OnPostRollBackAsync(int to, int version, CancellationToken cancellationToken)
    {
        if (to < 1 || version < 1)
        {
            return this.BadRequest();
        }

        OrganizationRollBackRequest request = new OrganizationRollBackRequest
        {
            Id = new OrganizationId(this.Input.Id),
            Version = new VersionNumber(version),
            To = new VersionNumber(to),
            ChangedBy = SignedInUser.Of(this.User),
        };
        UpdateOutcome outcome = await this.Organizations.RollBackAsync(request, cancellationToken).ConfigureAwait(false);
        OrganizationSingleResponse? organization = await this.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return this.ShowGone();
        }

        // Undo rolls back again, to the version this one replaced, from the version the roll-back made.
        string? undo = outcome == UpdateOutcome.Saved
            ? this.HandlerAddress(RollBackHandler, new { id = this.Input.Id, to = version, version = version + 1 })
            : null;
        return this.Answer(this.RollBackMessage(outcome, to, organization.Name), undo);
    }

    /// <inheritdoc/>
    protected override async Task ReadMoreAsync(OrganizationId id, CancellationToken cancellationToken)
    {
        this.Versions = await this.Organizations.ListVersionsAsync(id, cancellationToken).ConfigureAwait(false);
        OrganizationSingleResponse organization = this.Organization
            ?? throw new InvalidOperationException("The versions are read after the organization.");
        this.Timeline = new Timeline
        {
            RecordName = organization.Name.Value,
            Current = organization.Version,
            Entries = OrganizationChanges.Entries(organization, this.Versions),
            RollBackAction = this.HandlerAddress(RollBackHandler, new { id = this.Input.Id }),
            Target = this.ContentTarget,
        };
    }

    /// <summary>
    /// Writes the toast's message for what became of a roll-back.
    /// </summary>
    /// <param name="outcome">What became of it.</param>
    /// <param name="to">The version it was to.</param>
    /// <param name="name">The organization's name now.</param>
    /// <returns>The message.</returns>
    private string RollBackMessage(UpdateOutcome outcome, int to, OrganizationName name)
    {
        return outcome switch
        {
            UpdateOutcome.Saved =>
                $"{name.Value} rolled back to version {to}.",
            UpdateOutcome.Unchanged =>
                $"{name.Value} already holds the values of version {to}.",
            UpdateOutcome.Archived =>
                $"{name.Value} is archived, so it can't be rolled back until it is restored.",
            UpdateOutcome.NameTaken =>
                $"Another organization is now called {this.NameIn(to)}, so {name.Value} can't go back to version {to}.",
            UpdateOutcome.NotFound =>
                $"{name.Value} has no version {to}.",
            _ =>
                $"{name.Value} has changed since its history was shown. Check the newest version, then try again.",
        };
    }

    /// <summary>
    /// Gives the name an organization held at one of its versions.
    /// </summary>
    /// <param name="number">The version's number.</param>
    /// <returns>The name, or "that name" when the version is not among those read.</returns>
    private string NameIn(int number)
    {
        OrganizationVersionResponse? version = this.Versions.FirstOrDefault(version => version.Version.Value == number);
        return version?.Name.Value ?? "that name";
    }
}
