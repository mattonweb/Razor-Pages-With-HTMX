namespace HTMX.Test.Unit.Application.Web.Pages.Contacts;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Pages.Contacts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the contacts' <see cref="EditModel"/> class: how the form is
/// filled, what a save sends and answers, and the two forms of the organization field: the select that works without
/// HTMX, and the relation field and picker HTMX loads in its place.
/// </summary>
[TestClass]
public sealed class EditModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the form's heading takes the focus when HTMX loaded the page, as a boosted link or Back
    /// does, since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the focus.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "Loaded by the browser")]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "Loaded by a boosted link")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), true, DisplayName = "Loaded by Back")]
    public async Task OnGetAsync_LoadedByHtmx_FocusesTheHeading(string kind, bool focused)
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubContactService(found: CreateContact()), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        await page.OnGetAsync(10001, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            focused,
            page.HeadingFocused,
            "Only a page that HTMX loaded should put the keyboard on its heading.");
    }

    /// <summary>
    /// Unit test to verify that the form is filled from the contact, with the organization as a select of the
    /// organizations a contact can belong to, which HTMX enhances from the address the field names.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_KnownContact_FillsTheFormAndTheSelect()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubContactService(found: CreateContact()), HtmxRequest.None);

        // Act (When)
        await page.OnGetAsync(10001, CancellationToken.None).ConfigureAwait(false);
        ContactFields? fields = page.Fields;

        // Assert (Then)
        Assert.AreEqual(
            ("Ainsley Archetype", (long?)10003, 4),
            (page.Input.Name, page.Input.OrganizationId, page.Input.Version),
            "The form should start from the contact's values and version.");
        Assert.AreEqual(
            (2, (OrganizationField?)null),
            (fields?.OrganizationOptions.Count, fields?.Relation),
            "The organization should be a select of both organizations a contact can belong to.");
        Assert.AreEqual(
            "/Contacts/Edit/10001?handler=OrganizationField&organization=10003",
            fields?.EnhanceAction,
            "HTMX should load the relation field for the contact's organization.");
    }

    /// <summary>
    /// Unit test to verify that an identifier no contact has gets the page saying so, with 404.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_UnknownContact_AnswersNotFound()
    {
        // Arrange (Given)
        EditModel page = CreatePage(new StubContactService(), HtmxRequest.None);

        // Act (When)
        await page.OnGetAsync(99999, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            StatusCodes.Status404NotFound,
            page.Response.StatusCode,
            "The page should say no contact has the identifier, with 404.");
    }

    /// <summary>
    /// Unit test to verify that a save HTMX sent sends the values trimmed, an empty value as none, and answers 204 with
    /// HX-Location naming the contact's page.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_SavedByHtmx_SendsTheValuesAndAsksHtmxToLoadTheContact()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(found: CreateContact());
        EditModel page = CreatePage(contacts, HtmxRequest.Partial);
        page.Input = CreateInput(organization: 10002);

        // Act (When)
        IActionResult result = await page.OnPostAsync(10001, CancellationToken.None).ConfigureAwait(false);
        ContactUpdateRequest? sent = contacts.LastUpdate();

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "HTMX should get a status, not content.");
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, "/Contacts/Details/10001"),
            (status.StatusCode, page.Response.Headers["HX-Location"].ToString()),
            "HTMX should be asked to load the contact's page.");
        Assert.AreEqual(
            ("Ainsley Archetype", (JobTitle?)null, new OrganizationId(10002), new VersionNumber(4)),
            (sent?.Name.Value, sent?.JobTitle, sent?.Organization, sent?.Version),
            "The name should be trimmed, an empty job title sent as none, with the organization and the version.");
    }

    /// <summary>
    /// Unit test to verify that a save refused because the organization takes no one in marks the organization field,
    /// and answers with 422: with HTMX, the edit page's content with the relation field and picker drawn at once;
    /// without it, the whole page with the select.
    /// </summary>
    /// <param name="sentByHtmx">Whether HTMX sent the save.</param>
    /// <param name="expected">The kind of answer.</param>
    [TestMethod]
    [DataRow(true, typeof(PartialViewResult), DisplayName = "With HTMX")]
    [DataRow(false, typeof(PageResult), DisplayName = "Without HTMX")]
    public async Task OnPostAsync_OrganizationUnavailable_MarksTheOrganizationField(bool sentByHtmx, Type expected)
    {
        // Arrange (Given)
        StubContactService contacts =
            new StubContactService(outcome: UpdateOutcome.OrganizationUnavailable, found: CreateContact());
        EditModel page = CreatePage(contacts, sentByHtmx ? HtmxRequest.Partial : HtmxRequest.None);
        page.Input = CreateInput(organization: 10009);

        // Act (When)
        IActionResult result = await page.OnPostAsync(10001, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType(
            result,
            expected,
            "HTMX should get the content alone, a browser the whole page.");
        Assert.AreEqual(
            (StatusCodes.Status422UnprocessableEntity, ModelValidationState.Invalid),
            (page.Response.StatusCode, page.ModelState.GetFieldValidationState("Input.OrganizationId")),
            "The organization field should carry the reason, with 422.");
        Assert.AreEqual(
            sentByHtmx,
            page.Fields?.Relation is not null && page.Picker is not null,
            "Only an answer to HTMX should draw the relation field and the picker at once.");
    }

    /// <summary>
    /// Unit test to verify that a save another save overtook keeps the values being edited, shows the saved ones, and
    /// starts again from the saved version, with 409.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_Overtaken_ShowsTheSavedValuesFromTheSavedVersion()
    {
        // Arrange (Given)
        ContactSingleResponse saved = CreateContact(version: 5);
        StubContactService contacts = new StubContactService(outcome: UpdateOutcome.Overtaken, found: saved);
        EditModel page = CreatePage(contacts, HtmxRequest.Partial);
        page.Input = CreateInput(organization: 10003);

        // Act (When)
        await page.OnPostAsync(10001, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (StatusCodes.Status409Conflict, 5, true),
            (page.Response.StatusCode, page.Input.Version, page.Overtaken),
            "The form should come back with 409, starting from version 5.");
        Assert.AreSame(
            saved,
            page.Fields?.Saved,
            "The fields should show the values the other save left.");
    }

    /// <summary>
    /// Unit test to verify that a save to a contact archived meanwhile, or whose organization was, comes back with 409,
    /// to show the alert that says they cannot be edited, and that the alert takes the keyboard, since the form and its
    /// Save button are gone.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_Archived_AnswersConflictAndFocusesTheAlert()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(outcome: UpdateOutcome.Archived, found: CreateContact());
        EditModel page = CreatePage(contacts, HtmxRequest.Partial);
        page.Input = CreateInput(organization: 10003);

        // Act (When)
        IActionResult result = await page.OnPostAsync(10001, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "The edit page's content should come back to swap in place.");
        Assert.AreEqual(
            (StatusCodes.Status409Conflict, true, false),
            (page.Response.StatusCode, page.ArchivedMeanwhile, page.Overtaken),
            "A save to an archived contact should be answered with 409, the alert taking the keyboard.");
    }

    /// <summary>
    /// Unit test to verify that the field handler answers with the relation field for the organization chosen and
    /// the picker, whose button takes the focus after a choice.
    /// </summary>
    [TestMethod]
    public async Task OnGetOrganizationFieldAsync_Chosen_AnswersTheFieldAndThePicker()
    {
        // Arrange (Given)
        StubOrganizationService organizations = new StubOrganizationService(CreateFictionalFreight());
        EditModel page = CreatePage(new StubContactService(), HtmxRequest.Partial, organizations);

        // Act (When)
        IActionResult result = await page
            .OnGetOrganizationFieldAsync(10001, 10002, choose: true, CancellationToken.None)
            .ConfigureAwait(false);

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the field to swap in.");
        Assert.AreEqual(
            ("_OrganizationFieldAnswer", "Fictional Freight", true),
            (partial.ViewName, page.Relation?.Chosen?.Name.Value, page.Relation?.Focus),
            "The answer should hold the field for Fictional Freight, with the focus on its button.");
        Assert.AreEqual(
            ((OrganizationId?)new OrganizationId(10002), "/Contacts/Edit/10001?handler=OrganizationChoices"),
            (page.Picker?.Chosen, page.Picker?.ChoicesAction),
            "The picker should check Fictional Freight and send its filter to the choices handler.");
    }

    /// <summary>
    /// Unit test to verify that the picker's filter reads the first ten active organizations whose names contain it,
    /// by name, and answers with the list alone.
    /// </summary>
    [TestMethod]
    public async Task OnGetOrganizationChoicesAsync_Filter_ReadsTheFirstActiveMatches()
    {
        // Arrange (Given)
        StubOrganizationService organizations = new StubOrganizationService();
        EditModel page = CreatePage(new StubContactService(), HtmxRequest.Partial, organizations);

        // Act (When)
        IActionResult result = await page
            .OnGetOrganizationChoicesAsync(10001, "fict", 10003, CancellationToken.None)
            .ConfigureAwait(false);
        OrganizationCollectionRequest? sent = organizations.LastRequest();

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the list to swap in.");
        Assert.AreEqual(
            ("_OrganizationChoices", "fict", RecordStatus.Active, new PageSize(10)),
            (partial.ViewName, sent?.Filter?.Value, sent?.Status, sent?.PageSize),
            "The list should hold the first ten active organizations that match the filter.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Ainsley Archetype at Placeholder Logistics, with invented values.
    /// </summary>
    /// <param name="version">The version they are at.</param>
    /// <returns>The contact.</returns>
    private static ContactSingleResponse CreateContact(int version = 4)
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new ContactSingleResponse
        {
            Id = new ContactId(10001),
            Name = new DisplayName("Ainsley Archetype"),
            JobTitle = new JobTitle("Head of Examples"),
            Organization = new OrganizationReference
            {
                Id = new OrganizationId(10003),
                Name = new OrganizationName("Placeholder Logistics"),
            },
            Version = new VersionNumber(version),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates Fictional Freight as the organization service reads it, active, with invented values.
    /// </summary>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreateFictionalFreight()
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(10002),
            Name = new OrganizationName("Fictional Freight"),
            Industry = Industry.Shipping,
            Contacts = new RecordCount(0),
            Version = new VersionNumber(1),
            Created = change,
            LastModified = change,
        };
    }

    /// <summary>
    /// Creates the form as the browser would post it from version 4: the name with spaces around it and the job title
    /// emptied.
    /// </summary>
    /// <param name="organization">The organization chosen.</param>
    /// <returns>The form's values.</returns>
    private static EditModel.InputModel CreateInput(long organization)
    {
        return new EditModel.InputModel
        {
            Version = 4,
            Name = "  Ainsley Archetype ",
            JobTitle = " ",
            OrganizationId = organization,
        };
    }

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and a stand-in for routing. The organization service offers two organizations to choose.
    /// </summary>
    /// <param name="contacts">The contact service the page reads and saves through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <param name="organizations">The organization service, or <see langword="null"/> for one that offers two
    /// choices and finds Placeholder Logistics.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static EditModel CreatePage(
        StubContactService contacts,
        HtmxRequest kind,
        StubOrganizationService? organizations = null)
    {
        OrganizationReference[] choices =
        [
            new OrganizationReference
            {
                Id = new OrganizationId(10002),
                Name = new OrganizationName("Fictional Freight"),
            },
            new OrganizationReference
            {
                Id = new OrganizationId(10003),
                Name = new OrganizationName("Placeholder Logistics"),
            },
        ];
        PageContext context = PageRequests.Create(kind, "/Contacts/Edit");
        return new EditModel(contacts, organizations ?? new StubOrganizationService(choices: choices))
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
