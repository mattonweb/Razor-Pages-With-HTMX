namespace HTMX.Application.Web;

using System;
using HTMX.Application.Web.Data;
using HTMX.Application.Web.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/// <summary>
/// The application's entry point and composition root: it registers the services, creates the database and builds
/// the request pipeline.
/// </summary>
public static class Program
{
    /// <summary>
    /// The name of the application's in-memory database, which is created empty each time the application starts.
    /// </summary>
    private static readonly DatabaseName ApplicationDatabaseName = new DatabaseName("HtmxPages");

    /// <summary>
    /// Starts the application and runs it until it is stopped.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        AddServices(builder.Services);

        WebApplication application = builder.Build();
        CreateDatabase(application.Services);
        UsePipeline(application);
        application.Run();
    }

    /// <summary>
    /// Registers Razor Pages, the in-memory database and the context that reads and writes it.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    private static void AddServices(IServiceCollection services)
    {
        services.AddRazorPages();

        // A singleton made by a factory, so the container disposes it, and so the database, when the application stops.
        services.AddSingleton(_ => new InMemoryDatabase(ApplicationDatabaseName));
        services.AddDbContext<ApplicationDatabaseContext>(ConfigureContext);
    }

    /// <summary>
    /// Points a new context at the application's in-memory database.
    /// </summary>
    /// <param name="serviceProvider">The provider the context is being created from.</param>
    /// <param name="options">The options of the context being configured.</param>
    private static void ConfigureContext(IServiceProvider serviceProvider, DbContextOptionsBuilder options)
    {
        InMemoryDatabase database = serviceProvider.GetRequiredService<InMemoryDatabase>();
        database.Configure(options);
    }

    /// <summary>
    /// Creates the database's tables from the entity model, before the first request arrives.
    /// </summary>
    /// <param name="services">The application's root service provider.</param>
    private static void CreateDatabase(IServiceProvider services)
    {
        using (IServiceScope scope = services.CreateScope())
        {
            ApplicationDatabaseContext context = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();
            context.Database.EnsureCreated();
        }
    }

    /// <summary>
    /// Builds the request pipeline: error handling, HTTPS, static files and the Razor Pages.
    /// </summary>
    /// <param name="application">The application being built.</param>
    private static void UsePipeline(WebApplication application)
    {
        if (!application.Environment.IsDevelopment())
        {
            application.UseExceptionHandler("/Error");
            application.UseHsts();
        }

        application.UseHttpsRedirection();
        application.MapStaticAssets();

        PageActionEndpointConventionBuilder pages = application.MapRazorPages();
        pages.WithStaticAssets();
    }
}
