namespace HTMX.Test.Integration;

using System;
using System.Linq;
using System.Net.Http;
using HTMX.Application.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Runs the whole application in memory for one test: its own routing, antiforgery check, Content Security Policy and
/// pages, on a test server, with an in-memory database of its own, created and seeded as the application starts.
/// </summary>
/// <remarks>
/// The application keeps its database in SQLite's shared memory under a name, and two hosts in one process with the
/// same name would share one database. So each factory names its own through the configuration key the application
/// reads, and a test that changes a record changes it for itself alone.
/// </remarks>
internal sealed class ApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"Customers-{Guid.NewGuid():N}";

    /// <summary>
    /// Creates a client that keeps cookies, as a browser does, so that the antiforgery cookie a page sets goes back
    /// with the form, and that hands back every redirect as it came, so that a test can see it.
    /// </summary>
    /// <returns>The client. The factory disposes it with itself.</returns>
    public HttpClient CreateBrowser()
    {
        WebApplicationFactoryClientOptions options = new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        };
        return this.CreateClient(options);
    }

    /// <summary>
    /// Starts the application on Kestrel, at a port of its own on this machine, so that a real browser can reach it.
    /// It stops with the factory. Call it before anything else starts the application, such as a client.
    /// </summary>
    /// <returns>The application's address, such as <c>http://127.0.0.1:54321</c>.</returns>
    public Uri StartServerForBrowser()
    {
        this.UseKestrel(0);
        this.StartServer();
        IServer server = this.Services.GetRequiredService<IServer>();
        IServerAddressesFeature addresses = server.Features.GetRequiredFeature<IServerAddressesFeature>();
        return new Uri(addresses.Addresses.First());
    }

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("Customers:DatabaseName", this._databaseName);
    }
}
