namespace HTMX.Test.Integration.Application.Web.Pages.Components;

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the Long job page, at /components/long-job, run through the whole application: a job
/// started with HTMX, whose card polls its own address.
/// </summary>
[TestClass]
public sealed class LongJobTests : IDisposable
{
    #region Private Fields

    // The page's address, which its form also posts to.
    private const string Address = "/components/long-job";

    // The application, started on the first request with a database of its own and stopped when the test ends.
    private readonly ApplicationFactory _application = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that Start, sent by HTMX, answers the card polling every second and pushes the job's
    /// address, and that a poll of that address gets the card alone, still polling.
    /// </summary>
    [TestMethod]
    public async Task Post_StartedByHtmx_AnswersACardThatPolls()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(browser, Address).ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, []);

        // Act (When)
        Answer started = await Requests.PostAsync(browser, Address, HtmxRequest.Partial, form).ConfigureAwait(false);
        Answer poll = await Requests.GetAsync(browser, Address + "?job=10001", HtmxRequest.Partial)
            .ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.OK, "/components/long-job?job=10001", HttpStatusCode.OK, false),
            (started.Status, started.Headers["HX-Push-Url"], poll.Status, Html.IsWholePage(poll.Body)),
            "Start should answer the card and push the job's address, and a poll should get the card alone.");
        Assert.Contains(
            "hx-trigger=\"every 1s\"",
            poll.Body,
            "The card should poll while the job runs.");
    }

    /// <summary>
    /// Integration test to verify that a number no job has gets the page saying so, with 404.
    /// </summary>
    [TestMethod]
    public async Task Get_UnknownJob_Answers404()
    {
        // Arrange (Given)
        HttpClient browser = this._application.CreateBrowser();

        // Act (When)
        Answer answer = await Requests.GetAsync(browser, Address + "?job=99999", HtmxRequest.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            (HttpStatusCode.NotFound, true),
            (answer.Status, answer.Body.Contains("No job has the number 99999", StringComparison.Ordinal)),
            "An unknown job should get the page saying so, with 404.");
    }

    /// <summary>
    /// Stops the application and deletes its database.
    /// </summary>
    public void Dispose()
    {
        this._application.Dispose();
    }

    #endregion Public Methods
}
