namespace HTMX.Application.Web.Pages;

using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Where a toast waits in TempData for the next page to show it: across the redirect that follows an action sent
/// without HTMX, or across the page HTMX loads by <c>HX-Location</c>, such as the list a deleted record leaves for.
/// TempData keeps a value until it is read, so the toast shows once.
/// </summary>
public static class TempDataToasts
{
    private const string MessageKey = "ToastMessage";
    private const string UndoKey = "ToastUndo";
    private const string UndoTargetKey = "ToastUndoTarget";

    /// <summary>
    /// Leaves a toast for the next page to show.
    /// </summary>
    /// <param name="tempData">The request's TempData.</param>
    /// <param name="toast">The toast.</param>
    public static void Leave(ITempDataDictionary tempData, Toast toast)
    {
        ArgumentNullException.ThrowIfNull(tempData);
        ArgumentNullException.ThrowIfNull(toast);
        tempData[MessageKey] = toast.Message;
        tempData[UndoKey] = toast.UndoAction;
        tempData[UndoTargetKey] = toast.UndoTarget;
    }

    /// <summary>
    /// Takes the toast an earlier request left, if any, so that this page shows it and the next does not.
    /// </summary>
    /// <param name="tempData">The request's TempData.</param>
    /// <param name="undoTarget">The element its Undo replaces when the toast left none, such as
    /// <c>#organization</c>.</param>
    /// <returns>The toast, or <see langword="null"/> when none was left.</returns>
    public static Toast? Take(ITempDataDictionary tempData, string undoTarget)
    {
        ArgumentNullException.ThrowIfNull(tempData);
        if (tempData[MessageKey] is not string message)
        {
            return null;
        }

        string? undo = tempData[UndoKey] as string;
        string target = tempData[UndoTargetKey] as string ?? undoTarget;
        return new Toast
        {
            Message = message,
            UndoAction = undo,
            UndoTarget = undo is null ? null : target,
        };
    }
}
