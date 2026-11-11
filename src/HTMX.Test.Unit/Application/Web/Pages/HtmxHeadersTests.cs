namespace HTMX.Test.Unit.Application.Web.Pages;

using HTMX.Application.Web.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="HtmxHeaders"/> class: the HTMX headers the pages
/// read and write.
/// </summary>
[TestClass]
public sealed class HtmxHeadersTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that saying the response varies by HX-Request-Type keeps a Vary that another part of the
    /// application set first, so its cache variation survives.
    /// </summary>
    [TestMethod]
    public void VaryByRequestType_VaryAlreadySet_KeepsItAndAddsRequestType()
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();
        HttpResponse response = http.Response;
        IHeaderDictionary headers = response.Headers;
        headers.Vary = "Accept-Encoding";

        // Act (When)
        HtmxHeaders.VaryByRequestType(response);

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { "Accept-Encoding", "HX-Request-Type" },
            headers.Vary.ToArray(),
            "The response should keep the Vary already set and add HX-Request-Type after it.");
    }

    #endregion Public Methods
}
