namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Time.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="SignInExpiryModel"/> class: a page only a signed-in
/// visitor may see, whose card asks the server for the time.
/// </summary>
[TestClass]
public sealed class SignInExpiryModelTests
{
    #region Private Fields

    // The server's time in the tests, and when the visitor's sign-in ends, thirty seconds later.
    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 11, 13, 19, 48, 52, TimeSpan.Zero);
    private static readonly DateTimeOffset Ends = Now.AddSeconds(30);

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Unit test to verify that Check again, HTMX asking for part of the page, gets the card alone, with the server's
    /// time and when the sign-in ends, read from the visitor's claim.
    /// </summary>
    [TestMethod]
    public void OnGet_PartByHtmx_AnswersTheCard()
    {
        // Arrange (Given)
        SignInExpiryModel page = CreatePage(HtmxRequest.Partial);

        // Act (When)
        IActionResult result = page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            ("_SignInClock", Now, Ends, false),
            ((result as PartialViewResult)?.ViewName, page.Now, page.Ends, page.HeadingFocused),
            "Check again should get the card, with the time and when the sign-in ends.");
    }

    /// <summary>
    /// Unit test to verify that the whole page is drawn for a boosted link and for the browser on its own, with the
    /// heading taking the keyboard only for HTMX.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_WholePage_DrawsThePage(string kind, bool focused)
    {
        // Arrange (Given)
        SignInExpiryModel page = CreatePage(Enum.Parse<HtmxRequest>(kind));

        // Act (When)
        IActionResult result = page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            (true, focused, Ends),
            (result is PageResult, page.HeadingFocused, page.Ends),
            "The page should be drawn, its heading focused only for HTMX.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Creates the page as the framework would for one request from the signed-in visitor: the headers HTMX 4 sends for
    /// that kind of request, and a clock in UTC set to the tests' time.
    /// </summary>
    /// <param name="kind">What sent the request.</param>
    /// <returns>The page, ready for its handler to run.</returns>
    private static SignInExpiryModel CreatePage(HtmxRequest kind)
    {
        FakeTimeProvider clock = new FakeTimeProvider(Now);
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        PageContext context = PageRequests.Create(kind, "/Components/SignInExpiry");
        context.HttpContext.User = VisitorSignIn.Visitor(Ends);
        return new SignInExpiryModel(clock)
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
        };
    }

    #endregion Private Methods
}
