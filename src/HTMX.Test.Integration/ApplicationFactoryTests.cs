namespace HTMX.Test.Integration;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains integration tests for the <see cref="ApplicationFactory"/> class: every test's application starts from the
/// seed, whatever another test's application changed, so tests can run side by side.
/// </summary>
[TestClass]
public sealed class ApplicationFactoryTests : IDisposable
{
    #region Private Fields

    // The archived organizations whose name holds Placeholder Logistics, and the link to its page in that list.
    private const string ArchivedList = "/organizations?status=archived&filter=Placeholder%20Logistics";
    private const string PlaceholderLink = "href=\"/organizations/10003\"";

    // Two applications in one test run, as two tests running side by side would have.
    private readonly ApplicationFactory _changed = new ApplicationFactory();
    private readonly ApplicationFactory _untouched = new ApplicationFactory();

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Integration test to verify that an organization archived in one application is still active in another: each
    /// names a database of its own, which the application reads from its configuration.
    /// </summary>
    [TestMethod]
    public async Task CreateBrowser_TwoApplications_KeepTheirOwnDatabases()
    {
        // Arrange (Given)
        HttpClient changed = this._changed.CreateBrowser();
        HttpClient untouched = this._untouched.CreateBrowser();
        string token = await Requests.ReadAntiforgeryTokenAsync(changed, "/organizations").ConfigureAwait(false);
        IEnumerable<KeyValuePair<string, string>> form = Html.Form(token, new Dictionary<string, string>());
        await Requests.PostAsync(changed, "/organizations?organization=10003&handler=Archive", HtmxRequest.Partial, form)
            .ConfigureAwait(false);

        // Act (When)
        Answer archivedInChanged = await Requests.GetAsync(changed, ArchivedList, HtmxRequest.Partial)
            .ConfigureAwait(false);
        Answer archivedInUntouched = await Requests.GetAsync(untouched, ArchivedList, HtmxRequest.Partial)
            .ConfigureAwait(false);

        // Assert (Then)
        bool inChanged = archivedInChanged.Body.Contains(PlaceholderLink, StringComparison.Ordinal);
        bool inUntouched = archivedInUntouched.Body.Contains(PlaceholderLink, StringComparison.Ordinal);
        Assert.AreEqual(
            (true, false),
            (inChanged, inUntouched),
            "Placeholder Logistics should be archived in the application that archived it, and only there.");
    }

    /// <summary>
    /// Stops both applications and deletes their databases.
    /// </summary>
    public void Dispose()
    {
        this._changed.Dispose();
        this._untouched.Dispose();
    }

    #endregion Public Methods
}
