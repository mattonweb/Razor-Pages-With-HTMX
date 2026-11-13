namespace HTMX.Test.Unit.Application.Web.Pages;

using System;
using HTMX.Application.Web.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HomeModel"/> class: the home page.
/// </summary>
[TestClass]
public sealed class HomeModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page's heading takes the focus when HTMX loaded the page, as the brand's boosted
    /// link or Back does, since HTMX moves the keyboard nowhere itself, and not when the browser loaded it on its own.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the focus.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "Loaded by the browser")]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "Loaded by a boosted link")]
    [DataRow(nameof(HtmxRequest.HistoryRestore), true, DisplayName = "Loaded by Back")]
    public void OnGet_LoadedByHtmx_FocusesTheHeading(string kind, bool focused)
    {
        // Arrange (Given)
        HomeModel page = new HomeModel
        {
            PageContext = PageRequests.Create(Enum.Parse<HtmxRequest>(kind), "/Index"),
        };

        // Act (When)
        page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            focused,
            page.HeadingFocused,
            "Only a page that HTMX loaded should put the keyboard on its heading.");
    }

    #endregion Public Methods
}
