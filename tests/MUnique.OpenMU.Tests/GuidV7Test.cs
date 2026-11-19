// <copyright file="GuidV7Test.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.Persistence;

/// <summary>
/// Tests for the <see cref="GuidV7"/>.
/// </summary>
[TestFixture]
public class GuidV7Test
{
    /// <summary>
    /// Tests that the generated guids have the RFC 9562 variant and version 7.
    /// </summary>
    [Test]
    public void NewGuidHasRfcVariantAndVersion7()
    {
        for (var i = 0; i < 100; i++)
        {
            var guid = GuidV7.NewGuid();

            Assert.That(guid.Variant, Is.InRange(8, 0xB));
            Assert.That(guid.Version, Is.EqualTo(7));
        }
    }
}
