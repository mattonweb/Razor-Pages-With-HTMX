namespace HTMX.Test.Unit.Application.Web.Authentication;

using System;
using System.Globalization;
using System.Security.Claims;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="VisitorSignIn"/> class: the Sign-in expiry page's
/// thirty-second cookie sign-in.
/// </summary>
[TestClass]
public sealed class VisitorSignInTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the scheme's cookie names its sign-in page, lasts thirty seconds without sliding, and
    /// answers a request with no visitor through the event that tells HTMX from the browser.
    /// </summary>
    [TestMethod]
    public void Configure_Options_SetsTheSignIn()
    {
        // Arrange (Given)
        CookieAuthenticationOptions options = new CookieAuthenticationOptions();

        // Act (When)
        VisitorSignIn.Configure(options);

        // Assert (Then)
        Assert.AreEqual(
            ("/components/visitor-sign-in", TimeSpan.FromSeconds(30), false),
            (options.LoginPath.Value, options.ExpireTimeSpan, options.SlidingExpiration),
            "The cookie should name its sign-in page and last thirty seconds.");
        Assert.AreEqual(
            HtmxCookieEvents.RedirectToLogin,
            options.Events.OnRedirectToLogin,
            "A request with no visitor should be answered by the event that tells HTMX from the browser.");
    }

    /// <summary>
    /// Unit test to verify that the visitor carries an identifier, a name and when the sign-in ends, which reads back
    /// as the same moment.
    /// </summary>
    [TestMethod]
    public void Visitor_Ends_CarriesWhenTheSignInEnds()
    {
        // Arrange (Given)
        DateTimeOffset ends = new DateTimeOffset(2026, 11, 13, 19, 49, 22, TimeSpan.Zero);

        // Act (When)
        ClaimsPrincipal visitor = VisitorSignIn.Visitor(ends);
        string? claim = visitor.FindFirstValue(VisitorSignIn.EndsClaim);
        CultureInfo invariant = CultureInfo.InvariantCulture;
        DateTimeStyles roundTrip = DateTimeStyles.RoundtripKind;
        bool read = DateTimeOffset.TryParse(claim, invariant, roundTrip, out DateTimeOffset readBack);

        // Assert (Then)
        Assert.AreEqual(
            ("visitor", "Visitor", true, ends),
            (visitor.FindFirstValue(ClaimTypes.NameIdentifier), visitor.Identity?.Name, read, readBack),
            "The visitor should be named, and say when the sign-in ends.");
    }

    #endregion Public Methods
}
