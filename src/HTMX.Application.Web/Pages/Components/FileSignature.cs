namespace HTMX.Application.Web.Pages.Components;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Tells what kind of file was sent from its first bytes, which the file's own format fixes, rather than from its name
/// or the type the browser claims, either of which the sender chooses.
/// </summary>
public static class FileSignature
{
    // The bytes each kind of file the form takes starts with.
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D];

    /// <summary>
    /// Names the kind of a file the form takes: a PNG or JPEG image, or a PDF document.
    /// </summary>
    /// <param name="file">The file as the form sent it.</param>
    /// <param name="cancellationToken">Signals that the browser has gone away.</param>
    /// <returns>The kind, such as <c>a PNG image</c>, or <see langword="null"/> for any other file.</returns>
    public static async Task<string?> KindOfAsync(IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);
        byte[] start = new byte[Png.Length];
        int read;
        using (Stream content = file.OpenReadStream())
        {
            read = await content.ReadAtLeastAsync(start, start.Length, throwOnEndOfStream: false, cancellationToken)
                .ConfigureAwait(false);
        }

        return KindOf(start, read);
    }

    /// <summary>
    /// Names the kind of file its first bytes say it is.
    /// </summary>
    /// <param name="start">The file's first bytes.</param>
    /// <param name="read">How many of them the file had, which is fewer for a file shorter than the longest
    /// signature.</param>
    /// <returns>The kind, or <see langword="null"/> for any other file.</returns>
    private static string? KindOf(byte[] start, int read)
    {
        ReadOnlySpan<byte> first = start.AsSpan(0, read);
        if (first.StartsWith(Png))
        {
            return "a PNG image";
        }

        if (first.StartsWith(Jpeg))
        {
            return "a JPEG image";
        }

        return first.StartsWith(Pdf) ? "a PDF document" : null;
    }
}
