namespace HTMX.Test.Unit.Application.Web.Authorization;

using System;
using System.Linq;
using HTMX.Application.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="PublishingPolicy"/> class: the policy a page asks
/// before it offers or runs an action that publishes.
/// </summary>
[TestClass]
public sealed class PublishingPolicyTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that the policy is registered under its name and asks for a signed-in user who may publish.
    /// </summary>
    [TestMethod]
    public void Configure_Options_AddsThePolicyForASignedInPublisher()
    {
        // Arrange (Given)
        AuthorizationOptions options = new AuthorizationOptions();

        // Act (When)
        PublishingPolicy.Configure(options);
        AuthorizationPolicy? policy = options.GetPolicy(PublishingPolicy.Name);
        Type[] requirements = policy is null ? [] : [.. policy.Requirements.Select(static item => item.GetType())];

        // Assert (Then)
        CollectionAssert.AreEquivalent(
            new[] { typeof(DenyAnonymousAuthorizationRequirement), typeof(PublishingRequirement) },
            requirements,
            "The policy should ask for a signed-in user who may publish.");
    }

    #endregion Public Methods
}
