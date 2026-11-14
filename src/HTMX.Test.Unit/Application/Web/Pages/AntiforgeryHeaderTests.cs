namespace HTMX.Test.Unit.Application.Web.Pages;

using System;
using HTMX.Application.Web.Pages;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="AntiforgeryHeader"/> class: the antiforgery token
/// as an <c>hx-headers</c> value, for a request no form could send.
/// </summary>
[TestClass]
public sealed class AntiforgeryHeaderTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the value is JSON naming the header the antiforgery check reads, with the request's
    /// token, so that HTMX reads it as it is.
    /// </summary>
    [TestMethod]
    public void HxHeaders_HeaderNamed_WritesItWithTheToken()
    {
        // Arrange (Given)
        AntiforgeryTokenSet tokens = new AntiforgeryTokenSet("token-1", "cookie-1", "field", "X-Token");
        StubAntiforgery antiforgery = new StubAntiforgery(tokens);

        // Act (When)
        string headers = AntiforgeryHeader.HxHeaders(antiforgery, new DefaultHttpContext());

        // Assert (Then)
        Assert.AreEqual(
            "{\"X-Token\":\"token-1\"}",
            headers,
            "The value should name the header and hold the token.");
    }

    /// <summary>
    /// Unit test to verify that an application whose antiforgery check reads no header gets an exception, since a
    /// request with no form would then have no way to send the token.
    /// </summary>
    [TestMethod]
    public void HxHeaders_NoHeaderNamed_Throws()
    {
        // Arrange (Given)
        AntiforgeryTokenSet tokens = new AntiforgeryTokenSet("token-1", "cookie-1", "field", null);
        StubAntiforgery antiforgery = new StubAntiforgery(tokens);

        // Act (When)
        InvalidOperationException thrown = Assert.ThrowsExactly<InvalidOperationException>(
            () => AntiforgeryHeader.HxHeaders(antiforgery, new DefaultHttpContext()),
            "With no header to send, there should be no value.");

        // Assert (Then)
        Assert.IsNotNull(
            thrown.Message,
            "The exception should say why.");
    }

    #endregion Public Methods
}
