namespace HTMX.Application.Web;

using System;
using System.Threading.Tasks;
using HTMX.Adapter.Customers;
using HTMX.Adapter.Customers.Configuration;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.Pages.Components;
using HTMX.Application.Web.Security;
using Microsoft.AspNetCore.Authentication;
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
    /// Registers Razor Pages, with every URL they write in lower case, the sign-in scheme, the customers adapter, which
    /// owns the database, and the Long job page's jobs.
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

        string? configuredName = configuration[CustomersDatabaseKey];
        CustomersOptions customers = new CustomersOptions
        {
            DatabaseName = configuredName is null ? DefaultCustomersDatabaseName : new DatabaseName(configuredName),
        };
        services.AddCustomersAdapter(customers);

        // The Long job page's pretend jobs, one set for the application, counted by the system clock.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<LongJobs>();
    }

    /// <summary>
    /// Builds the request pipeline: error handling, HTTPS, the Content Security Policy, static files, sign-in and the
    /// Razor Pages.
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

        PageActionEndpointConventionBuilder pages = application.MapRazorPages();
        pages.WithStaticAssets();
    }
}
