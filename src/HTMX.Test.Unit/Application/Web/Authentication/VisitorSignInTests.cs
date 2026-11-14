namespace HTMX.Test.Unit.Application.Web.Authentication;

using System;
using System.Globalization;
using System.Security.Claims;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="VisitorSignIn"/> class: the visitors' thirty-second
/// cookie sign-in, with a pass or without one.
/// </summary>
[TestClass]
public sealed class VisitorSignInTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the scheme's cookie names its sign-in and access-denied pages, lasts thirty seconds
    /// without sliding, and answers a request with no visitor, and one whose visitor may not see the page, through the
    /// events that tell HTMX from the browser.
    /// </summary>
    [TestMethod]
    public void Configure_Options_SetsTheSignIn()
    {
        // Arrange (Given)
        CookieAuthenticationOptions options = new CookieAuthenticationOptions();

        // Act (When)
        VisitorSignIn.Configure(options);
        string? deniedPath = options.AccessDeniedPath.Value;

        // Assert (Then)
        Assert.AreEqual(
            ("/components/visitor-sign-in", "/components/visitor-access-denied", TimeSpan.FromSeconds(30), false),
            (options.LoginPath.Value, deniedPath, options.ExpireTimeSpan, options.SlidingExpiration),
            "The cookie should name its sign-in and access-denied pages and last thirty seconds.");
        Assert.AreEqual(
            HtmxCookieEvents.RedirectToLogin,
            options.Events.OnRedirectToLogin,
            "A request with no visitor should be answered by the event that tells HTMX from the browser.");
        Assert.AreEqual(
            HtmxCookieEvents.RedirectToAccessDenied,
            options.Events.OnRedirectToAccessDenied,
            "A visitor who may not see the page should be answered by the event that tells HTMX from the browser.");
    }

    /// <summary>
    /// Unit test to verify that the visitor carries an identifier, a name and when the sign-in ends, which reads back
    /// as the same moment, and no pass when signed in without one.
    /// </summary>
    [TestMethod]
    public void Visitor_Ends_CarriesWhenTheSignInEnds()
    {
        // Arrange (Given)
        DateTimeOffset ends = new DateTimeOffset(2026, 11, 13, 19, 49, 22, TimeSpan.Zero);

        // Act (When)
        ClaimsPrincipal visitor = VisitorSignIn.Visitor(ends, pass: false);
        string? claim = visitor.FindFirstValue(VisitorSignIn.EndsClaim);
        CultureInfo invariant = CultureInfo.InvariantCulture;
        DateTimeStyles roundTrip = DateTimeStyles.RoundtripKind;
        bool read = DateTimeOffset.TryParse(claim, invariant, roundTrip, out DateTimeOffset readBack);

        // Assert (Then)
        Assert.AreEqual(
            ("visitor", "Visitor", true, ends),
            (visitor.FindFirstValue(ClaimTypes.NameIdentifier), visitor.Identity?.Name, read, readBack),
            "The visitor should be named, and say when the sign-in ends.");
        Assert.IsFalse(
            visitor.HasClaim(static claim => claim.Type == VisitorSignIn.PassClaim),
            "A visitor signed in without a pass should carry none.");
    }

    /// <summary>
    /// Unit test to verify that a visitor signed in with a pass carries it, as the Visitors' room asks.
    /// </summary>
    [TestMethod]
    public void Visitor_WithAPass_CarriesThePass()
    {
        // Arrange (Given)
        DateTimeOffset ends = new DateTimeOffset(2026, 11, 14, 15, 26, 9, TimeSpan.Zero);

        // Act (When)
        ClaimsPrincipal visitor = VisitorSignIn.Visitor(ends, pass: true);

        // Assert (Then)
        Assert.IsTrue(
            visitor.HasClaim(static claim => claim.Type == VisitorSignIn.PassClaim),
            "A visitor signed in with a pass should carry it.");
    }

    #endregion Public Methods
}
