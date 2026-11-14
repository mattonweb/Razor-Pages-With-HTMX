namespace HTMX.Application.Web.Pages.Components;

/// <summary>
/// The Order lines page's form as it posts: its lines, in order. The lines bind by their position, so the form's
/// positions must run from 0 without a gap; the binding stops at the first one missing.
/// </summary>
public sealed class OrderInput
{
    /// <summary>
    /// Gets or sets the lines; a new form has one, empty.
    /// </summary>
    public OrderLineInput[] Lines { get; set; } = [new OrderLineInput()];
}
