namespace HTMX.Application.Web.Authorization;

using System;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// The Publishing policy, which a page asks through <see cref="IAuthorizationService"/> before it offers or runs an
/// action that publishes: a signed-in user whom <see cref="Publishers"/> names.
/// </summary>
public static class PublishingPolicy
{
    /// <summary>
    /// The name the policy is registered under, which a page asks for.
    /// </summary>
    public const string Name = "Publishing";

    /// <summary>
    /// Adds the policy to the application's authorization options.
    /// </summary>
    /// <param name="options">The options.</param>
    public static void Configure(AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddPolicy(Name, Build);
    }

    /// <summary>
    /// Builds the policy: a signed-in user, who may publish.
    /// </summary>
    /// <param name="policy">The policy being built.</param>
    private static void Build(AuthorizationPolicyBuilder policy)
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PublishingRequirement());
    }
}
