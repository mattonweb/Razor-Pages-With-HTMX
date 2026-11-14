namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using System.Reflection;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.Authorization;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="VisitorsRoomModel"/> class: a page only a visitor
/// with a pass may see, whose card asks the server for the time with HTMX.
/// </summary>
[TestClass]
public sealed class VisitorsRoomModelTests
{
    #region Private Fields

    // The server's time when the page is asked for.
    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 11, 14, 15, 26, 9, TimeSpan.Zero);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page asks for the Visitors' pass policy, so that the refusal comes before its
    /// handler runs.
    /// </summary>
    [TestMethod]
    public void VisitorsRoomModel_Attributes_AskForTheVisitorPassPolicy()
    {
        // Arrange (Given)
        Type page = typeof(VisitorsRoomModel);

        // Act (When)
        AuthorizeAttribute? authorize = page.GetCustomAttribute<AuthorizeAttribute>();

        // Assert (Then)
        Assert.AreEqual(
            VisitorPassPolicy.Name,
            authorize?.Policy,
            "The page should ask for the Visitors' pass policy.");
    }

    /// <summary>
    /// Unit test to verify that Check again, HTMX asking for part of the page, gets the card alone, with the time.
    /// </summary>
    [TestMethod]
    public void OnGet_PartByHtmx_AnswersTheCard()
    {
        // Arrange (Given)
        VisitorsRoomModel page = CreatePage(HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            ("_RoomCard", Now, false),
            ((result as PartialViewResult)?.ViewName, page.Now, page.HeadingFocused),
            "HTMX should get the card alone, with the time.");
    }

    /// <summary>
    /// Unit test to verify that the whole page is drawn for a boosted link and for the browser on its own, its heading
    /// taking the keyboard only for HTMX.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_WholePage_DrawsThePage(string kind, bool focused)
    {
        // Arrange (Given)
        VisitorsRoomModel page = CreatePage(Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            (true, focused),
            (result is PageResult, page.HeadingFocused),
            "The whole page should be drawn, its heading focused only for HTMX.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request from a visitor with a pass: the headers HTMX 4 sends
    /// for that kind of request, and a clock that says <see cref="Now"/>.
    /// </summary>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static VisitorsRoomModel CreatePage(HtmxRequest kind)
    {
        FakeTimeProvider clock = new FakeTimeProvider(Now);
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        PageContext context = PageRequests.Create(kind, "/Components/VisitorsRoom");
        context.HttpContext.User = VisitorSignIn.Visitor(Now.AddSeconds(30), pass: true);
        return new VisitorsRoomModel(clock)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
        };
    }

    #endregion Private Methods
}
