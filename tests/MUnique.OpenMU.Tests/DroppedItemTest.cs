// <copyright file="DroppedItemTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Tests for the remove timer of the <see cref="DroppedItem"/> and <see cref="DroppedMoney"/>.
/// </summary>
[TestFixture]
public class DroppedItemTest
{
    private const byte ChunkSize = 8;

    /// <summary>
    /// Tests that the remove timer of a dropped item starts when the map adds it, not when it is created.
    /// Before, an item whose drop duration elapsed before it was added was disposed too early
    /// and then stayed on the map forever.
    /// </summary>
    [Test]
    public async Task RemoveTimerStartsWhenTheItemIsAddedAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.Zero, ChunkSize);
        var droppedItem = new DroppedItem(new TemporaryItem(), new Point(100, 100), map, null, null);
        await Task.Delay(100).ConfigureAwait(false);
        Assert.That(droppedItem.IsDisposed, Is.False);

        await map.AddAsync(droppedItem).ConfigureAwait(false);
        var id = droppedItem.Id;

        Assert.That(await WaitUntilAsync(() => droppedItem.IsDisposed && map.GetDrop(id) is null).ConfigureAwait(false), Is.True);
    }

    /// <summary>
    /// Tests that disposing a dropped item which was never added to the map doesn't remove the other drop
    /// which holds the drop id 0, the id of a drop which is not on the map.
    /// </summary>
    [Test]
    public async Task DisposingAnItemWhichWasNotAddedKeepsTheOtherDropAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromMinutes(1), ChunkSize);
        var otherDrop = new DroppedItem(new TemporaryItem(), new Point(50, 50), map, null, null);
        await map.AddAsync(otherDrop).ConfigureAwait(false);
        Assert.That(otherDrop.Id, Is.EqualTo(0), "test setup: the other drop must hold the id 0");

        await new DroppedItem(new TemporaryItem(), new Point(100, 100), map, null, null).DisposeAsync().ConfigureAwait(false);

        Assert.That(map.GetDrop(0), Is.SameAs(otherDrop));
    }

    /// <summary>
    /// Tests that disposing dropped money which was never added to the map doesn't remove the other drop
    /// which holds the drop id 0, the id of a drop which is not on the map.
    /// </summary>
    [Test]
    public async Task DisposingMoneyWhichWasNotAddedKeepsTheOtherDropAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromMinutes(1), ChunkSize);
        var otherDrop = new DroppedMoney(1000, new Point(50, 50), map);
        await map.AddAsync(otherDrop).ConfigureAwait(false);
        Assert.That(otherDrop.Id, Is.EqualTo(0), "test setup: the other drop must hold the id 0");

        await new DroppedMoney(500, new Point(100, 100), map).DisposeAsync().ConfigureAwait(false);

        Assert.That(map.GetDrop(0), Is.SameAs(otherDrop));
    }

    private static async Task<bool> WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                return false;
            }

            await Task.Delay(10).ConfigureAwait(false);
        }

        return true;
    }
}
