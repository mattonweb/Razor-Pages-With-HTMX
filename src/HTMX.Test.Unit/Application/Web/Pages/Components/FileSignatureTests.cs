namespace HTMX.Test.Unit.Application.Web.Pages.Components;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HTMX.Application.Web.Pages.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Contains unit tests for verifying the behavior of the <see cref="FileSignature"/> class: what kind a file is, by
/// its first bytes rather than its name.
/// </summary>
[TestClass]
public sealed class FileSignatureTests
{
    #region Public Methods

    /// <summary>
    /// Unit test to verify that each kind the form takes is told by its first bytes, and anything else, a file too
    /// short for any signature included, is no kind at all.
    /// </summary>
    /// <param name="hex">The file's bytes, in hexadecimal.</param>
    /// <param name="expected">The kind expected, or <see langword="null"/>.</param>
    [TestMethod]
    [DataRow("89504E470D0A1A0A0000", "a PNG image", DisplayName = "PNG")]
    [DataRow("FFD8FFE000104A464946", "a JPEG image", DisplayName = "JPEG")]
    [DataRow("255044462D312E37", "a PDF document", DisplayName = "PDF")]
    [DataRow("68656C6C6F", null, DisplayName = "Text")]
    [DataRow("8950", null, DisplayName = "Shorter than a signature")]
    public async Task KindOfAsync_EachKindOfFile_NamesItByItsFirstBytes(string hex, string? expected)
    {
        // Arrange (Given)
        byte[] content = Convert.FromHexString(hex);
        FormFile file = new FormFile(new MemoryStream(content), 0, content.Length, "Input.File", "named.png");

        // Act (When)
        string? kind = await FileSignature.KindOfAsync(file, CancellationToken.None).ConfigureAwait(false);

        // Assert (Then)
        Assert.AreEqual(
            expected,
            kind,
            "The kind should come from the file's first bytes, whatever its name.");
    }

    #endregion Public Methods
}
