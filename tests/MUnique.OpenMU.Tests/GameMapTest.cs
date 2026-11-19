// <copyright file="GameMapTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Nito.AsyncEx;

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Tests for the game map.
/// </summary>
[TestFixture]
public class GameMapTest
{
    private const byte ChunkSize = 8;

    /// <summary>
    /// An interface which combines several other interfaces which are needed in combination for this test.
    /// </summary>
    public interface ITestPlayer : ILocateable, IBucketMapObserver, IObservable, ISupportIdUpdate
    {
    }

    /// <summary>
    /// Tests if the discovery of players works when a new player is entering the map in the view range.
    /// </summary>
    [Test]
    public async ValueTask TestPlayerEntersMapAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var player1 = this.GetPlayer();
        player1.Object.Position = new Point(100, 100);
        await map.AddAsync(player1.Object).ConfigureAwait(false);
        var player2 = this.GetPlayer();
        player2.Object.Position = new Point(101, 100);
        await map.AddAsync(player2.Object).ConfigureAwait(false);
        player1.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player2.Object))), Times.Once);
        player2.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player1.Object))), Times.Once);
        player1.Verify(p => p.LocateableAddedAsync(It.IsAny<ILocateable>()), Times.Once);
    }

    /// <summary>
    /// Tests if movements of a player into the view range of another player causes
    /// that the players get notified about each other as soon as they are in view range.
    /// </summary>
    [Test]
    public async ValueTask TestPlayerMovesInMapAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var player1 = this.GetPlayer();

        await map.AddAsync(player1.Object).ConfigureAwait(false);
        var player2 = this.GetPlayer();
        player2.Object.Position = new Point(101, 100);
        await map.AddAsync(player2.Object).ConfigureAwait(false);

        await map.MoveAsync(player1.Object, new Point(100, 100), new AsyncLock(), 0).ConfigureAwait(false);

        player1.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player2.Object))), Times.Once);
        player1.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player1.Object))), Times.Once);
        player2.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player1.Object))), Times.Once);
        player2.Verify(p => p.LocateableAddedAsync(It.IsAny<ILocateable>()), Times.Once);
    }

    /// <summary>
    /// Tests if movements of a player out of the view range of another player causes
    /// that the players get notified about it.
    /// </summary>
    [Test]
    public async ValueTask PlayerMovesOutOfRangeAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var player1 = this.GetPlayer();
        player1.Object.Position = new Point(101, 100);
        await map.AddAsync(player1.Object).ConfigureAwait(false);
        var player2 = this.GetPlayer();
        player2.Object.Position = new Point(101, 100);
        await map.AddAsync(player2.Object).ConfigureAwait(false);

        await map.MoveAsync(player1.Object, new Point(100, 130), new AsyncLock(), 0).ConfigureAwait(false);
        player1.Verify(p => p.LocateablesOutOfScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player2.Object))), Times.Once);
        player2.Verify(p => p.LocateableRemovedAsync(It.IsAny<ILocateable>()), Times.Once);
    }

    /// <summary>
    /// Tests if movements of a player into and out of the view range of another player causes
    /// that the players get notified about it.
    /// </summary>
    [Test]
    public async ValueTask PlayerMovesOutAndIntoTheRangeAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var player1 = this.GetPlayer();
        player1.Object.Position = new Point(101, 100);
        await map.AddAsync(player1.Object).ConfigureAwait(false);
        var player2 = this.GetPlayer();
        player2.Object.Position = new Point(101, 100);
        await map.AddAsync(player2.Object).ConfigureAwait(false);

        player1.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player2.Object))), Times.Once);
        player2.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player1.Object))), Times.Once);
        player1.Verify(p => p.LocateableAddedAsync(It.IsAny<ILocateable>()), Times.Once);
        player1.Invocations.Clear();
        player2.Invocations.Clear();

        await map.MoveAsync(player1.Object, new Point(100, 130), new AsyncLock(), 0).ConfigureAwait(false);
        player1.Verify(p => p.LocateablesOutOfScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player2.Object))), Times.Once);
        player2.Verify(p => p.LocateableRemovedAsync(It.IsAny<ILocateable>()), Times.Once);
        player1.Invocations.Clear();
        player2.Invocations.Clear();

        await map.MoveAsync(player2.Object, new Point(101, 130), new AsyncLock(), 0).ConfigureAwait(false);
        player2.Verify(p => p.NewLocateablesInScopeAsync(It.Is<IEnumerable<ILocateable>>(n => n.Contains(player1.Object))), Times.Once);
        player1.Verify(p => p.LocateableAddedAsync(It.IsAny<ILocateable>()), Times.Once);
    }

    /// <summary>
    /// Tests the performance of the movements. Not a standard test.
    /// </summary>
    /// [Test]
    public async ValueTask TestPerformanceMoveAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var player1 = this.GetPlayer();
        await map.AddAsync(player1.Object).ConfigureAwait(false);
        var player2 = this.GetPlayer();
        player2.Object.Position = new Point(101, 100);
        await map.AddAsync(player2.Object).ConfigureAwait(false);

        var sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        var moveLock = new AsyncLock();
        for (int i = 0; i < 1000; i++)
        {
            await map.MoveAsync(player1.Object, new Point((byte)(100 + (i % 30)), (byte)(100 + (i % 30))), moveLock, 0).ConfigureAwait(false);
        }

        sw.Stop();
        Console.WriteLine(sw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Tests if the players in view range get notified when another player leaves the map.
    /// </summary>
    [Test]
    public async ValueTask TestPlayerLeavesMapAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var player1 = this.GetPlayer();
        player1.Object.Position = new Point(100, 100);
        await map.AddAsync(player1.Object).ConfigureAwait(false);
        var player2 = this.GetPlayer();
        player2.Object.Position = new Point(101, 100);
        await map.AddAsync(player2.Object).ConfigureAwait(false);
        await map.RemoveAsync(player2.Object).ConfigureAwait(false);
        Assert.AreEqual(player2.Object.ObservingBuckets.Count, 0);
        player1.Verify(p => p.LocateableRemovedAsync(It.IsAny<ILocateable>()), Times.Once);
        player2.Verify(p => p.LocateableRemovedAsync(It.IsAny<ILocateable>()), Times.Once);
        Assert.That(player1.Object.Observers.Count, Is.EqualTo(0));
        Assert.That(player2.Object.Observers.Count, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that tearing down a map releases the registrations it owns.
    /// </summary>
    [Test]
    public async ValueTask TearDownReleasesOwnedRegistrationsAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var registration = new Mock<IDisposable>();
        map.RegisterOwnedRegistration(registration.Object);
        registration.Verify(r => r.Dispose(), Times.Never);

        await map.TearDownAsync().ConfigureAwait(false);

        registration.Verify(r => r.Dispose(), Times.Once);
    }

    /// <summary>
    /// Tests that a registration which is added to an already torn down map, e.g. by a spawn which raced the
    /// teardown, is released right away instead of being kept by the map.
    /// </summary>
    [Test]
    public async ValueTask RegistrationOfTornDownMapIsReleasedImmediatelyAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        await map.TearDownAsync().ConfigureAwait(false);
        var registration = new Mock<IDisposable>();

        map.RegisterOwnedRegistration(registration.Object);

        registration.Verify(r => r.Dispose(), Times.Once);
    }

    /// <summary>
    /// Tests that removing the drop with id 0, the first drop id of a map, gives the id back and raises
    /// <see cref="GameMap.ObjectRemoved"/>, like for any other drop.
    /// </summary>
    [Test]
    public async ValueTask RemovingTheDropWithIdZeroGivesItsIdBackAndRaisesObjectRemovedAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var drop = new DroppedMoney(1000, new Point(100, 100), map);
        await map.AddAsync(drop).ConfigureAwait(false);
        Assert.That(drop.Id, Is.EqualTo(0), "test setup: the first drop of a map must get id 0");
        var removed = new List<ILocateable>();
        map.ObjectRemoved += args =>
        {
            removed.Add(args.Object);
            return ValueTask.CompletedTask;
        };

        await map.RemoveAsync(drop).ConfigureAwait(false);

        Assert.That(removed, Is.EqualTo(new[] { drop }));
        var nextDrop = new DroppedMoney(1000, new Point(100, 100), map);
        await map.AddAsync(nextDrop).ConfigureAwait(false);
        Assert.That(nextDrop.Id, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that removing an object which is not on the map, but has the id 0, keeps the drop which is stored under id 0.
    /// </summary>
    [Test]
    public async ValueTask RemovingAnObjectWhichIsNotOnTheMapKeepsTheDropWithIdZeroAsync()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromSeconds(60), ChunkSize);
        var drop = new DroppedMoney(1000, new Point(100, 100), map);
        await map.AddAsync(drop).ConfigureAwait(false);
        var removedRaised = false;
        map.ObjectRemoved += _ =>
        {
            removedRaised = true;
            return ValueTask.CompletedTask;
        };
        var neverAdded = this.GetPlayer();
        Assert.That(neverAdded.Object.Id, Is.EqualTo(0), "test setup: the object must have the id of the drop");

        await map.RemoveAsync(neverAdded.Object).ConfigureAwait(false);

        Assert.That(map.GetDrop(0), Is.SameAs(drop));
        Assert.That(removedRaised, Is.False);
    }

    private Mock<ITestPlayer> GetPlayer()
    {
        var player = new Mock<ITestPlayer>();
        player.SetupAllProperties();
        player.As<ILocateable>().SetupGet(p => p.Id).Returns(() => (player.Object as ISupportIdUpdate).Id);

        player.Setup(p => p.ObservingBuckets).Returns(new List<Bucket<ILocateable>>());
        player.Setup(p => p.Observers).Returns(new HashSet<IWorldObserver>());
        player.Setup(p => p.ObserverLock).Returns(new AsyncReaderWriterLock());
        player.Setup(p => p.InfoRange).Returns(20);
        player.Setup(p => p.AddObserverAsync(It.IsAny<IWorldObserver>())).Callback<IWorldObserver>(o => player.Object.Observers.Add(o));
        player.Setup(p => p.RemoveObserverAsync(It.IsAny<IWorldObserver>())).Callback<IWorldObserver>(o => player.Object.Observers.Remove(o));
        return player;
    }
}