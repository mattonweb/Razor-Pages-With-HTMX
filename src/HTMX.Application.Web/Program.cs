namespace HTMX.Application.Web;

using System;
using System.Threading.Tasks;
using HTMX.Adapter.Customers;
using HTMX.Adapter.Customers.Configuration;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.Authorization;
using HTMX.Application.Web.Pages.Components;
using HTMX.Application.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The application's entry point and composition root: it registers the services, builds the request pipeline, and
/// decides what runs before the first request.
/// </summary>
/// <remarks>
/// A sealed class rather than a static one, so that the integration tests can name the application they start as
/// <c>WebApplicationFactory&lt;Program&gt;</c>: a static class cannot be a type argument. Nothing creates one.
/// </remarks>
public sealed class Program
{
    /// <summary>
    /// The configuration key that names the customers adapter's in-memory database. Nothing sets it when the
    /// application runs; each integration test sets a name of its own, so that two hosts in one test run never share
    /// a database.
    /// </summary>
    private const string CustomersDatabaseKey = "Customers:DatabaseName";

    /// <summary>
    /// The name of the customers adapter's in-memory database, which is created each time the application starts,
    /// when configuration names no other.
    /// </summary>
    private static readonly DatabaseName DefaultCustomersDatabaseName = new DatabaseName("Customers");

    /// <summary>
    /// Prevents a default instance of the <see cref="Program"/> class from being created: it has only static members.
    /// </summary>
    private Program()
    {
    }

    /// <summary>
    /// Builds the application, creates the customer database, and runs the application until it is stopped.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A task that completes when the application has stopped.</returns>
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        AddServices(builder.Services, builder.Configuration);

        WebApplication application = builder.Build();
        UsePipeline(application);

        // The host is the application's, so it decides when the adapter's database is made: here, once, before the
        // application runs and so before the first request can arrive.
        ICustomersDatabase customersDatabase = application.Services.GetRequiredService<ICustomersDatabase>();
        await customersDatabase.CreateAsync().ConfigureAwait(false);
        await application.RunAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Registers Razor Pages, with every URL they write in lower case, the sign-in schemes and authorization, the
    /// customers adapter, which owns the database, and the Components pages' own state.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">The application's configuration, which may name the database.</param>
    private static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRazorPages();

        // Links and form actions come out as /organizations, not /Organizations; a request matches either way.
        // Query string values keep their case (LowercaseQueryStrings stays off), so a filter keeps its capitals.
        services.AddRouting(static options => options.LowercaseUrls = true);

        // Every request is signed in as Demo User, so a page reads who made a change from its User, as it would behind
        // a real sign-in.
        AuthenticationBuilder authentication = services.AddAuthentication(DemoAuthenticationHandler.SchemeName);
        authentication.AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
            DemoAuthenticationHandler.SchemeName,
            configureOptions: null);

        // The Sign-in expiry page's own sign-in, a cookie that lasts thirty seconds, and the authorization that sends a
        // request without it to the sign-in page. The Publishing policy, which the Not allowed page asks before it
        // offers or runs an action that publishes, is met for the users the Publishing permission page says may.
        authentication.AddCookie(VisitorSignIn.SchemeName, VisitorSignIn.Configure);
        services.AddAuthorization(PublishingPolicy.Configure);
        services.AddSingleton<Publishers>();
        services.AddSingleton<IAuthorizationHandler, PublishingHandler>();

        string? configuredName = configuration[CustomersDatabaseKey];
        CustomersOptions customers = new CustomersOptions
        {
            DatabaseName = configuredName is null ? DefaultCustomersDatabaseName : new DatabaseName(configuredName),
        };
        services.AddCustomersAdapter(customers);

        // The Components pages' own state, one of each for the application: the Long job page's pretend jobs,
        // counted by the system clock, the Reload on event page's notes, the Stock table page's shelf, the Seen
        // notices page's board and the Not allowed page's articles.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LongJobs>();
        services.AddSingleton<NoteBoard>();
        services.AddSingleton<StockShelf>();
        services.AddSingleton<NoticeBoard>();
        services.AddSingleton<ArticleDesk>();
    }

    /// <summary>
    /// Builds the request pipeline: error handling, HTTPS, the Content Security Policy, static files, sign-in, the
    /// authorization that pages asking for a sign-in need, and the Razor Pages.
    /// </summary>
    /// <param name="application">The application being built.</param>
    private static void UsePipeline(WebApplication application)
    {
        if (!application.Environment.IsDevelopment())
        {
            application.UseExceptionHandler("/error");
            application.UseHsts();
        }

        application.UseHttpsRedirection();

        // Every answer, a static file's included, tells the browser what its page may load and run.
        application.UseContentSecurityPolicy(application.Environment);
        application.MapStaticAssets();
        application.UseAuthentication();
        application.UseAuthorization();

        PageActionEndpointConventionBuilder pages = application.MapRazorPages();
        pages.WithStaticAssets();
    }
}
