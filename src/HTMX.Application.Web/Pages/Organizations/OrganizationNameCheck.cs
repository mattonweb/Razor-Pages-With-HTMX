namespace HTMX.Application.Web.Pages.Organizations;

using System;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Adapter.Customers.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// The rule that no two organizations share a name, as the create and edit forms show it: on a save the service
/// refused for it, and on the name alone while it is typed. Both say it in the same words, on the same field.
/// </summary>
public static class OrganizationNameCheck
{
    /// <summary>
    /// The form's name for the Name field, as ModelState keys it: both forms bind their values to <c>Input</c>.
    /// </summary>
    public const string NameKey = "Input." + nameof(OrganizationInput.Name);

    // Names match however they are capitalized, so the message does not repeat the one typed.
    private const string TakenMessage = "Another organization already has this name.";

    /// <summary>
    /// Marks the Name field as taken by another organization.
    /// </summary>
    /// <param name="modelState">The page's ModelState, which the field's message is drawn from.</param>
    public static void MarkTaken(ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(modelState);
        modelState.AddModelError(NameKey, TakenMessage);
    }

    /// <summary>
    /// Checks the name alone, as it is typed. A name that passed the field's own rules, such as its length, is looked
    /// up, and marked when another organization has it; a name that failed them keeps their message, and nothing is
    /// looked up. The other fields' messages are left for the save, since only the Name field is drawn again.
    /// </summary>
    /// <param name="organizations">Says whether another organization has the name.</param>
    /// <param name="input">The form's values, as bound from the post.</param>
    /// <param name="except">The organization being edited, whose own name does not count, or
    /// <see langword="null"/> for a new one.</param>
    /// <param name="modelState">The page's ModelState, which the field's message is drawn from.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>A task that completes when the name is checked.</returns>
    public static async Task CheckAsync(
        IOrganizationService organizations,
        OrganizationInput input,
        OrganizationId? except,
        ModelStateDictionary modelState,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(organizations);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(modelState);
        if (modelState.GetFieldValidationState(NameKey) != ModelValidationState.Valid)
        {
            return;
        }

        OrganizationName name = new OrganizationName(input.Name.Trim());
        bool taken = await organizations.IsNameTakenAsync(name, except, cancellationToken).ConfigureAwait(false);
        if (taken)
        {
            MarkTaken(modelState);
        }
    }
}
