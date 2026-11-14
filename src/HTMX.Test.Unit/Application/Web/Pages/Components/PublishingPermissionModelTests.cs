namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authorization;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="PublishingPermissionModel"/> class: whether the
/// signed-in user may publish, withdrawn and granted again with HTMX and without it.
/// </summary>
[TestClass]
public sealed class PublishingPermissionModelTests
{
    #region Private Fields

    // Demo User, whom every page test signs in.
    private static readonly UserId DemoUser = new UserId(10001);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page says whether the signed-in user may publish, and that its heading takes the
    /// keyboard only when HTMX loaded the whole page.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_EachKindOfRequest_SaysWhetherTheUserMayPublish(string kind, bool focused)
    {
        // Arrange (Given)
        PublishingPermissionModel page = CreatePage(new Publishers(), Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            (true, focused),
            (page.MayPublish, page.HeadingFocused),
            "The page should say Demo User may publish, the heading focused only for HTMX.");
    }

    /// <summary>
    /// Unit test to verify that withdrawing the permission with HTMX stops the user publishing, and the answer is the
    /// card as it is now and a toast.
    /// </summary>
    [TestMethod]
    public void OnPostWithdraw_ByHtmx_AnswersTheCardAndAToast()
    {
        // Arrange (Given)
        Publishers publishers = new Publishers();
        PublishingPermissionModel page = CreatePage(publishers, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnPostWithdraw();

        // Assert (Then)
        Assert.AreEqual(
            ("_PermissionChanged", false, "You may no longer publish articles.", false),
            ((result as PartialViewResult)?.ViewName, page.MayPublish, page.Toast?.Message, publishers.May(DemoUser)),
            "The permission should be withdrawn, and HTMX get the card and the toast.");
    }

    /// <summary>
    /// Unit test to verify that withdrawing the permission without HTMX is redirected back to the page, the toast left
    /// in TempData for it.
    /// </summary>
    [TestMethod]
    public void OnPostWithdraw_WithoutHtmx_RedirectsWithTheToast()
    {
        // Arrange (Given)
        Publishers publishers = new Publishers();
        PublishingPermissionModel page = CreatePage(publishers, HtmxRequest.None);

        // Act (When)
        IActionResult result = page.OnPostWithdraw();

        // Assert (Then)
        Assert.IsInstanceOfType<RedirectToPageResult>(
            result,
            "The browser should be sent back to the page.");
        Assert.AreEqual(
            ("You may no longer publish articles.", false),
            (page.TempData["ToastMessage"], publishers.May(DemoUser)),
            "The permission should be withdrawn, and the toast wait in TempData for the page.");
    }

    /// <summary>
    /// Unit test to verify that granting the permission again with HTMX lets the user publish, and the answer says so.
    /// </summary>
    [TestMethod]
    public void OnPostGrant_AfterAWithdrawal_LetsTheUserPublishAgain()
    {
        // Arrange (Given)
        Publishers publishers = new Publishers();
        publishers.Withdraw(DemoUser);
        PublishingPermissionModel page = CreatePage(publishers, HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnPostGrant();

        // Assert (Then)
        Assert.AreEqual(
            ("_PermissionChanged", true, "You may publish articles again.", true),
            ((result as PartialViewResult)?.ViewName, page.MayPublish, page.Toast?.Message, publishers.May(DemoUser)),
            "The permission should be granted, and HTMX get the card and the toast.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request: Demo User signed in, the headers HTMX 4 sends for that
    /// kind of request, and TempData.
    /// </summary>
    /// <param name="publishers">Who may publish.</param>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static PublishingPermissionModel CreatePage(Publishers publishers, HtmxRequest kind)
    {
        PageContext context = PageRequests.Create(kind, "/Components/PublishingPermission");
        return new PublishingPermissionModel(publishers)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            TempData = new TempDataDictionary(context.HttpContext, new StubTempDataProvider()),
        };
    }

    #endregion Private Methods
}
