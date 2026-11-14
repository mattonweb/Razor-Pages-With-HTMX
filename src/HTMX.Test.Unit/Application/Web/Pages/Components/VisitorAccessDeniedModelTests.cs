namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="VisitorAccessDeniedModel"/> class: where the
/// visitor's sign-in sends a visitor who may not see the page they asked for.
/// </summary>
[TestClass]
public sealed class VisitorAccessDeniedModelTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the page's heading takes the keyboard only when HTMX loaded the page, which it does not
    /// when it follows HX-Redirect, a full reload, and does for a boosted link.
    /// </summary>
    /// <param name="kind">What sent the request, by its name in <see cref="HtmxRequest"/>.</param>
    /// <param name="focused">Whether the heading should take the keyboard.</param>
    [TestMethod]
    [DataRow(nameof(HtmxRequest.Boosted), true, DisplayName = "A boosted link")]
    [DataRow(nameof(HtmxRequest.None), false, DisplayName = "The browser on its own")]
    public void OnGet_EachKindOfRequest_FocusesTheHeadingOnlyForHtmx(string kind, bool focused)
    {
        // Arrange (Given)
        PageContext context = PageRequests.Create(Enum.Parse<HtmxRequest>(kind), "/Components/VisitorAccessDenied");
        VisitorAccessDeniedModel page = new VisitorAccessDeniedModel
        {
            PageContext = context,
            MetadataProvider = new EmptyModelMetadataProvider(),
            ReturnUrl = "/components/visitors-room",
        };

        // Act (When)
        page.OnGet();

        // Assert (Then)
        Assert.AreEqual(
            focused,
            page.HeadingFocused,
            "The heading should take the keyboard only for HTMX.");
    }

    #endregion Public Methods
}
