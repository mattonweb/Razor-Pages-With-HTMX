namespace HTMX.Application.Web.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// What the Publishing policy asks of a request: that its user may publish, which <see cref="PublishingHandler"/>
/// checks against <see cref="Publishers"/>.
/// </summary>
public sealed class PublishingRequirement : IAuthorizationRequirement
{
}
