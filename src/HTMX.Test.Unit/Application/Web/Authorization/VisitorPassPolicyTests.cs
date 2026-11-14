namespace HTMX.Test.Unit.Application.Web.Authorization;

using System;
using HTMX.Application.Web.Authentication;
using HTMX.Application.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="VisitorPassPolicy"/> class: the Visitors' room's
/// policy, a visitor signed in with a pass.
/// </summary>
[TestClass]
public sealed class VisitorPassPolicyTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the policy is registered under its name, names the visitor's sign-in, so that its
    /// challenge and its forbid are that sign-in's, and asks for a signed-in visitor who carries a pass.
    /// </summary>
    [TestMethod]
    public void Configure_Options_AddsThePolicyForAVisitorWithAPass()
    {
        // Arrange (Given)
        AuthorizationOptions options = new AuthorizationOptions();

        // Act (When)
        VisitorPassPolicy.Configure(options);
        AuthorizationPolicy? policy = options.GetPolicy(VisitorPassPolicy.Name);
        string[] schemes = policy is null ? [] : [.. policy.AuthenticationSchemes];
        IAuthorizationRequirement[] requirements = policy is null ? [] : [.. policy.Requirements];
        ClaimsAuthorizationRequirement? pass = Array.Find(requirements, IsClaim) as ClaimsAuthorizationRequirement;

        // Assert (Then)
        CollectionAssert.AreEqual(
            new[] { VisitorSignIn.SchemeName },
            schemes,
            "The policy should name the visitor's sign-in.");
        Assert.AreEqual(
            (2, true, VisitorSignIn.PassClaim),
            (requirements.Length, Array.Exists(requirements, IsSignedIn), pass?.ClaimType),
            "The policy should ask for a signed-in visitor who carries a pass.");
    }

    #endregion Public Methods

    #region Private Methods

    /// <summary>
    /// Says whether a requirement asks for a claim.
    /// </summary>
    /// <param name="requirement">The requirement.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool IsClaim(IAuthorizationRequirement requirement)
    {
        return requirement is ClaimsAuthorizationRequirement;
    }

    /// <summary>
    /// Says whether a requirement asks for a signed-in user.
    /// </summary>
    /// <param name="requirement">The requirement.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool IsSignedIn(IAuthorizationRequirement requirement)
    {
        return requirement is DenyAnonymousAuthorizationRequirement;
    }

    #endregion Private Methods
}
