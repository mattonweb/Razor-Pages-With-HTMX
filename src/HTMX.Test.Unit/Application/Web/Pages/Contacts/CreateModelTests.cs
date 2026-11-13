namespace HTMX.Test.Unit.Application.Web.Pages.Contacts;

using System;
using System.Linq;
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
/// Contains unit tests for verifying the behavior of the contacts' <see cref="CreateModel"/> class: how the form starts
/// from the organization it was opened from, what a save sends, what each outcome answers with HTMX and without it,
/// and the organization field's addresses on a page that has no contact yet.
/// </summary>
[TestClass]
public sealed class CreateModelTests
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
        CreateModel page = CreatePage(new StubContactService(), Enum.Parse<HtmxRequest>(kind), CreateOrganizations());

        // Act (When)
        await page.OnGetAsync(null, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            focused,
            page.HeadingFocused,
            "Only a page that HTMX loaded should put the keyboard on its heading.");
    }

    /// <summary>
    /// Unit test to verify that a form opened from an active organization starts with it chosen and leads back to it,
    /// with the organization as a select that HTMX enhances from the create page's own address.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_OpenedFromAnActiveOrganization_ChoosesIt()
    {
        // Arrange (Given)
        StubOrganizationService organizations = CreateOrganizations(CreatePlaceholderLogistics(archived: false));
        CreateModel page = CreatePage(new StubContactService(), HtmxRequest.None, organizations);

        // Act (When)
        await page.OnGetAsync(10003, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((long?)10003, "Placeholder Logistics"),
            (page.Input.OrganizationId, page.OpenedFrom?.Name.Value),
            "The form should start with Placeholder Logistics chosen, and lead back to it.");
        Assert.AreEqual(
            (2, "/Contacts/Create?handler=OrganizationField&organization=10003"),
            (page.Fields?.OrganizationOptions.Count, page.Fields?.EnhanceAction),
            "The organization should be a select, which HTMX enhances from the create page, with no contact named.");
    }

    /// <summary>
    /// Unit test to verify that a form opened from an archived organization leads back to it but starts with no
    /// organization chosen, since saving would refuse it.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_OpenedFromAnArchivedOrganization_ChoosesNone()
    {
        // Arrange (Given)
        StubOrganizationService organizations = CreateOrganizations(CreatePlaceholderLogistics(archived: true));
        CreateModel page = CreatePage(new StubContactService(), HtmxRequest.None, organizations);

        // Act (When)
        await page.OnGetAsync(10003, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((long?)null, "Placeholder Logistics"),
            (page.Input.OrganizationId, page.OpenedFrom?.Name.Value),
            "The form should lead back to Placeholder Logistics with no organization chosen.");
    }

    /// <summary>
    /// Unit test to verify that a form opened from no organization looks none up, and starts with none chosen.
    /// </summary>
    [TestMethod]
    public async Task OnGetAsync_OpenedFromNone_ChoosesNone()
    {
        // Arrange (Given)
        StubOrganizationService organizations = CreateOrganizations(CreatePlaceholderLogistics(archived: false));
        CreateModel page = CreatePage(new StubContactService(), HtmxRequest.None, organizations);

        // Act (When)
        await page.OnGetAsync(null, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            ((OrganizationId?)null, (OrganizationSingleResponse?)null, (long?)null),
            (organizations.LastLookup(), page.OpenedFrom, page.Input.OrganizationId),
            "No organization should be looked up, led back to or chosen.");
        Assert.AreEqual(
            "/Contacts/Create?handler=OrganizationField",
            page.Fields?.EnhanceAction,
            "HTMX should load the relation field with no organization chosen.");
    }

    /// <summary>
    /// Unit test to verify that a new contact the browser sent without HTMX is redirected to their own page, and that
    /// what is sent is the form's values, trimmed, an empty value as none, from the signed-in user.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_CreatedWithoutHtmx_RedirectsToTheNewContact()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService();
        CreateModel page = CreatePage(contacts, HtmxRequest.None, CreateOrganizations());
        page.Input = CreateInput(organization: 10002);

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);
        ContactCreateRequest? sent = contacts.LastCreation();

        // Assert (Then)
        RedirectToPageResult redirect = Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "A new contact sent without HTMX should be redirected.");
        Assert.AreEqual(
            ("./Details", (object?)20001L),
            (redirect.PageName, redirect.RouteValues?["id"]),
            "The save should go to the page of the contact it created.");
        Assert.AreEqual(
            ("Quinn Quickstart", (JobTitle?)null, "quinn.quickstart@freight.example"),
            (sent?.Name.Value, sent?.JobTitle, sent?.Email?.Value),
            "The values should be trimmed, and the empty job title sent as none.");
        Assert.AreEqual(
            ((OrganizationId?)new OrganizationId(10002), (UserId?)new UserId(10001)),
            (sent?.Organization, sent?.CreatedBy),
            "The contact should belong to the organization chosen, created by the signed-in user.");
    }

    /// <summary>
    /// Unit test to verify that a new contact HTMX sent answers 204 with HX-Location naming their page.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_CreatedByHtmx_AsksHtmxToLoadTheNewContact()
    {
        // Arrange (Given)
        CreateModel page = CreatePage(new StubContactService(), HtmxRequest.Partial, CreateOrganizations());
        page.Input = CreateInput(organization: 10002);

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        StatusCodeResult status = Assert.IsInstanceOfType<StatusCodeResult>(
            result,
            "HTMX should get a status, not content.");
        Assert.AreEqual(
            (StatusCodes.Status204NoContent, "/Contacts/Details/20001"),
            (status.StatusCode, page.Response.Headers["HX-Location"].ToString()),
            "HTMX should be asked to load the new contact's page.");
    }

    /// <summary>
    /// Unit test to verify that a new contact refused because the organization takes no one in marks the organization
    /// field, and answers with 422 and the form, still leading back to the organization it was opened from: with HTMX,
    /// the form alone with the relation field and picker drawn at once; without it, the whole page with the select.
    /// </summary>
    /// <param name="sentByHtmx">Whether HTMX sent the save.</param>
    /// <param name="expected">The kind of answer.</param>
    [TestMethod]
    [DataRow(true, typeof(PartialViewResult), DisplayName = "With HTMX")]
    [DataRow(false, typeof(PageResult), DisplayName = "Without HTMX")]
    public async Task OnPostAsync_OrganizationUnavailable_MarksTheOrganizationField(bool sentByHtmx, Type expected)
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService(creation: CreateOutcome.OrganizationUnavailable);
        StubOrganizationService organizations = CreateOrganizations(CreatePlaceholderLogistics(archived: false));
        CreateModel page = CreatePage(contacts, sentByHtmx ? HtmxRequest.Partial : HtmxRequest.None, organizations);
        page.Input = CreateInput(organization: 10009);

        // Act (When)
        IActionResult result = await page.OnPostAsync(10003, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType(
            result,
            expected,
            "HTMX should get the form alone, a browser the whole page.");
        Assert.AreEqual(
            (StatusCodes.Status422UnprocessableEntity, ModelValidationState.Invalid),
            (page.Response.StatusCode, page.ModelState.GetFieldValidationState("Input.OrganizationId")),
            "The organization field should carry the reason, with 422.");
        Assert.AreEqual(
            "Placeholder Logistics",
            page.OpenedFrom?.Name.Value,
            "The form should still lead back to the organization it was opened from.");
        Assert.AreEqual(
            sentByHtmx,
            page.Fields?.Relation is not null && page.Picker is not null,
            "Only an answer to HTMX should draw the relation field and the picker at once.");
    }

    /// <summary>
    /// Unit test to verify that a form that fails validation sends nothing, and answers HTMX with the form alone and
    /// 422.
    /// </summary>
    [TestMethod]
    public async Task OnPostAsync_InvalidForm_SendsNothing()
    {
        // Arrange (Given)
        StubContactService contacts = new StubContactService();
        CreateModel page = CreatePage(contacts, HtmxRequest.Partial, CreateOrganizations());
        page.Input = CreateInput(organization: 10002);
        page.ModelState.AddModelError("Input.Name", "Enter a name.");

        // Act (When)
        IActionResult result = await page.OnPostAsync(null, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        PartialViewResult partial = Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the form to swap in.");
        Assert.AreEqual(
            ("_ContactCreateForm", StatusCodes.Status422UnprocessableEntity, (ContactCreateRequest?)null),
            (partial.ViewName, page.Response.StatusCode, contacts.LastCreation()),
            "The form should come back with 422, and nothing be sent.");
    }

    /// <summary>
    /// Unit test to verify that, on the create page, the field handler answers with the field and a picker whose
    /// addresses are the create page's, naming no contact.
    /// </summary>
    [TestMethod]
    public async Task OnGetOrganizationFieldAsync_NoContact_AnswersFromTheCreatePage()
    {
        // Arrange (Given)
        StubOrganizationService organizations = CreateOrganizations(CreatePlaceholderLogistics(archived: false));
        CreateModel page = CreatePage(new StubContactService(), HtmxRequest.Partial, organizations);

        // Act (When)
        IActionResult result = await page
            .OnGetOrganizationFieldAsync(null, 10003, choose: true, CancellationToken.None)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.IsInstanceOfType<PartialViewResult>(
            result,
            "HTMX should get the field to swap in.");
        Assert.AreEqual(
            "Placeholder Logistics",
            page.Relation?.Chosen?.Name.Value,
            "The field should hold Placeholder Logistics.");
        Assert.AreEqual(
            ("/Contacts/Create?handler=OrganizationChoices", "/Contacts/Create?handler=OrganizationField"),
            (page.Picker?.ChoicesAction, page.Picker?.FieldAction),
            "The picker should answer to the create page, naming no contact.");
    }

    /// <summary>
    /// Unit test to verify that Add on the create form posts back to the create page with the organization it was
    /// opened from, so that the redrawn page still leads back to it, and answers HTMX with the field: the one empty
    /// row a new form starts with, and the new one.
    /// </summary>
    [TestMethod]
    public async Task OnPostAddPhoneNumberAsync_ByHtmx_KeepsTheOrganizationItWasOpenedFrom()
    {
        // Arrange (Given)
        StubOrganizationService organizations = CreateOrganizations(CreatePlaceholderLogistics(archived: false));
        CreateModel page = CreatePage(new StubContactService(), HtmxRequest.Partial, organizations);
        page.Input = CreateInput(organization: 10003);
        page.Input.PhoneNumbers = [null];

        // Act (When)
        IActionResult result = await page.OnPostAddPhoneNumberAsync(10003, CancellationToken.None)
            .ConfigureAwait(false);
        PhoneNumberField? field = (result as PartialViewResult)?.Model as PhoneNumberField;

        // Assert (Then)
        Assert.AreEqual(
            ("/Contacts/Create?handler=AddPhoneNumber&organization=10003", 2),
            (field?.AddAction, field?.Rows.Length),
            "Add should post back to the create page opened from Placeholder Logistics, with one more row.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates Placeholder Logistics as the organization service reads it, with invented values.
    /// </summary>
    /// <param name="archived">Whether it is archived.</param>
    /// <returns>The organization.</returns>
    private static OrganizationSingleResponse CreatePlaceholderLogistics(bool archived)
    {
        RecordChange change = new RecordChange
        {
            On = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
            By = new UserReference { Id = new UserId(10001), Name = new DisplayName("Demo User") },
        };
        return new OrganizationSingleResponse
        {
            Id = new OrganizationId(10003),
            Name = new OrganizationName("Placeholder Logistics"),
            Industry = Industry.Shipping,
            Contacts = new RecordCount(16),
            Version = new VersionNumber(1),
            Created = change,
            LastModified = change,
            Archived = archived ? change : null,
        };
    }

    /// <summary>
    /// Creates the form as the browser would post it: Quinn Quickstart with spaces around the name and the email
    /// address, and the job title left blank.
    /// </summary>
    /// <param name="organization">The organization chosen.</param>
    /// <returns>The form's values.</returns>
    private static ContactInput CreateInput(long organization)
    {
        return new ContactInput
        {
            Name = "  Quinn Quickstart ",
            JobTitle = " ",
            Email = " quinn.quickstart@freight.example ",
            OrganizationId = organization,
        };
    }

    /// <summary>
    /// Creates the organization service, which offers two organizations to choose and finds the one given.
    /// </summary>
    /// <param name="found">The organization a lookup finds, or <see langword="null"/> for none.</param>
    /// <returns>The service.</returns>
    private static StubOrganizationService CreateOrganizations(OrganizationSingleResponse? found = null)
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
        return new StubOrganizationService(found, choices: choices);
    }

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and a stand-in for routing.
    /// </summary>
    /// <param name="contacts">The contact service the page creates through.</param>
    /// <param name="kind">What sent the request.</param>
    /// <param name="organizations">The organization service the page reads through.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static CreateModel CreatePage(
        StubContactService contacts,
        HtmxRequest kind,
        StubOrganizationService organizations)
    {
        PageContext context = PageRequests.Create(kind, "/Contacts/Create");
        return new CreateModel(contacts, organizations)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            Url = new StubUrlHelper(context),
        };
    }

    #endregion Private Methods
}
