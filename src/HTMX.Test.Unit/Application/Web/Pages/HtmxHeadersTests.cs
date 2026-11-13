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
    /// Unit test to verify that a request counts as HTMX's when it says HX-Request, and when it is HTMX 4.0.1's
    /// request for Back or Forward, which says HX-History-Restore-Request without HX-Request.
    /// </summary>
    /// <param name="htmxRequest">The HX-Request header's value, or <see langword="null"/> for none.</param>
    /// <param name="historyRestore">The HX-History-Restore-Request header's value, or <see langword="null"/> for
    /// none.</param>
    /// <param name="expected">Whether the request should count as HTMX's.</param>
    [TestMethod]
    [DataRow(null, null, false, DisplayName = "The browser on its own")]
    [DataRow("true", null, true, DisplayName = "A request HTMX sent")]
    [DataRow(null, "true", true, DisplayName = "Back or Forward, as HTMX 4.0.1 sends it")]
    public void IsHtmxRequest_EachKindOfRequest_TellsHtmxFromTheBrowser(
        string? htmxRequest,
        string? historyRestore,
        bool expected)
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();
        HttpRequest request = http.Request;
        IHeaderDictionary headers = request.Headers;
        headers["HX-Request"] = htmxRequest;
        headers["HX-History-Restore-Request"] = historyRestore;

        // Act (When)
        bool htmx = HtmxHeaders.IsHtmxRequest(request);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            htmx,
            "A request should count as HTMX's when either header says so.");
    }

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

    /// <summary>
    /// Unit test to verify that pushing an address writes it in HX-Push-Url, for HTMX to add to the history.
    /// </summary>
    [TestMethod]
    public void PushAddress_Address_WritesHxPushUrl()
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();
        HttpResponse response = http.Response;

        // Act (When)
        HtmxHeaders.PushAddress(response, "/components/long-job?job=10001");

        // Assert (Then)
        IHeaderDictionary headers = response.Headers;
        Assert.AreEqual(
            "/components/long-job?job=10001",
            headers["HX-Push-Url"].ToString(),
            "The address should go in HX-Push-Url.");
    }

    /// <summary>
    /// Unit test to verify that raising an event writes its name in HX-Trigger, for HTMX to raise on the page.
    /// </summary>
    [TestMethod]
    public void RaiseEvent_Name_WritesHxTrigger()
    {
        // Arrange (Given)
        DefaultHttpContext http = new DefaultHttpContext();
        HttpResponse response = http.Response;

        // Act (When)
        HtmxHeaders.RaiseEvent(response, "notes-changed");

        // Assert (Then)
        IHeaderDictionary headers = response.Headers;
        Assert.AreEqual(
            "notes-changed",
            headers["HX-Trigger"].ToString(),
            "The event's name should go in HX-Trigger.");
    }

    #endregion Public Methods
}
