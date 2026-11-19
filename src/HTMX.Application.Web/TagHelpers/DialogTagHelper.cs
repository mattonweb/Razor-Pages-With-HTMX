namespace HTMX.Application.Web.TagHelpers;

using System;
using Microsoft.AspNetCore.Razor.TagHelpers;

/// <summary>
/// Draws a dialog as the brand gives it, a native modal <c>&lt;dialog&gt;</c> over a scrim: <c>dialog</c> on the dialog
/// itself, or <c>dialog="confirm"</c> for a confirm dialog, which is narrower; then, inside it directly or inside one
/// form, its parts:
/// <list type="bullet">
/// <item><description><c>header</c>, which holds the h2 title and any close button;</description></item>
/// <item><description><c>body</c>, which scrolls on its own; <c>confirm-body</c>, a confirm dialog's, which sets its
/// icon beside its message; or <c>preview-body</c>, a preview dialog's, whose parts stand 12px apart;</description></item>
/// <item><description><c>icon</c>, a confirm dialog's icon in a circle of its tone, danger, or <c>icon-warning</c> or
/// <c>icon-info</c> for another;</description></item>
/// <item><description><c>message</c>, a confirm dialog's message, whose paragraphs and list stand 8px
/// apart;</description></item>
/// <item><description><c>footer</c>, which holds the buttons at the right.</description></item>
/// </list>
/// </summary>
/// <remarks>
/// Tailwind finds the class names it generates by reading this file, so each is written out whole. A form inside the
/// dialog adds no box of its own, so the header, body and footer lay out as the dialog's own.
/// </remarks>
[HtmlTargetElement("*", Attributes = Name)]
public sealed class DialogTagHelper : ComponentTagHelper
{
    private const string Name = "dialog";

    // What every dialog shares: the surface, never nearer than 64px to the window's top and bottom, a column of parts
    // once it is open, and the scrim behind it.
    private const string Shared =
        "max-h-[calc(100dvh_-_2_*_var(--space-8))] rounded-md border border-line bg-surface p-0 text-fg shadow-popup "
        + "backdrop:bg-scrim open:flex open:flex-col [&>form]:contents";

    private const string Dialog = Shared + " w-[min(560px,calc(100vw_-_2_*_var(--space-4)))]";

    private const string Confirm = Shared + " w-[min(420px,calc(100vw_-_2_*_var(--space-4)))]";

    private const string Header =
        "flex items-center justify-between gap-3 border-b border-line px-4 py-3 [&>h2]:text-lg";

    private const string Body = "flex-1 overflow-y-auto p-4";

    private const string ConfirmBody = Body + " flex items-start gap-3";

    private const string PreviewBody = Body + " grid gap-3";

    // What every icon shares: a 40px circle that centers its icon.
    private const string IconShared = "grid size-10 flex-none place-items-center rounded-full";

    private const string Icon = IconShared + " bg-danger-subtle text-danger";

    private const string IconWarning = IconShared + " bg-warning-subtle text-warning";

    private const string IconInfo = IconShared + " bg-info-subtle text-info";

    private const string Message = "grid gap-2 [&_ul]:m-0 [&_ul]:list-disc [&_ul]:pl-5";

    private const string Footer = "flex flex-wrap justify-end gap-2 border-t border-line px-4 py-3";

    /// <inheritdoc/>
    protected override string AttributeName => Name;

    /// <inheritdoc/>
    protected override string ClassesFor(string? part)
    {
        return part switch
        {
            null =>
                Dialog,
            "confirm" =>
                Confirm,
            "header" =>
                Header,
            "body" =>
                Body,
            "confirm-body" =>
                ConfirmBody,
            "preview-body" =>
                PreviewBody,
            "icon" =>
                Icon,
            "icon-warning" =>
                IconWarning,
            "icon-info" =>
                IconInfo,
            "message" =>
                Message,
            "footer" =>
                Footer,
            _ =>
                throw new InvalidOperationException(
                    "A dialog is a dialog or a confirm, and its parts are header, body, confirm-body, preview-body, "
                    + $"icon, icon-warning, icon-info, message and footer, not \"{part}\"."),
        };
    }
}
