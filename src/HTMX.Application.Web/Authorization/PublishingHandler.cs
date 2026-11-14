namespace HTMX.Application.Web.Authorization;

using System;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using HTMX.Application.Web.Authentication;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Meets the Publishing policy's requirement when the request's user is one <see cref="Publishers"/> names. It asks on
/// every request, so a permission withdrawn after a page was drawn refuses that page's next request.
/// </summary>
public sealed class PublishingHandler : AuthorizationHandler<PublishingRequirement>
{
    private readonly Publishers _publishers;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishingHandler"/> class.
    /// </summary>
    /// <param name="publishers">Who may publish.</param>
    public PublishingHandler(Publishers publishers)
    {
        ArgumentNullException.ThrowIfNull(publishers);
        this._publishers = publishers;
    }

    /// <summary>
    /// Meets the requirement when the request's user may publish, and leaves it unmet otherwise, a request with no
    /// signed-in user included.
    /// </summary>
    /// <param name="context">The request's user and what is being checked.</param>
    /// <param name="requirement">The requirement.</param>
    /// <returns>A task that is already complete.</returns>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PublishingRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        UserId? user = SignedInUser.Find(context.User);
        if (user is UserId signedIn && this._publishers.May(signedIn))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
