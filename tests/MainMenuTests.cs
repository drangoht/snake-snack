using System;
using SnakeSnack.Rules;
using Xunit;

namespace SnakeSnack.Tests;

/// <summary>
/// The composition and navigation of the main menu (GDD §4.6).
/// </summary>
public class MainMenuTests
{
    /// <summary>
    /// "Play" at the top: it is the only entry almost every visitor of an itch page will use, and it
    /// must sit under the cursor on the first frame.
    /// </summary>
    [Fact]
    public void PlayIsTheFirstEntry()
    {
        Assert.Equal(MenuEntry.Play, MainMenu.Entries(true)[0]);
        Assert.Equal(MenuEntry.Play, MainMenu.Entries(false)[0]);
    }

    /// <summary>
    /// ⚠ The case that motivates the parameter: on WebGL, <c>Application.Quit()</c> does nothing.
    /// The entry would be a dead button — the player clicks, nothing happens, and the whole menu
    /// loses its credibility.
    /// </summary>
    [Fact]
    public void QuitDisappearsWhenThePlatformCannotClose()
    {
        Assert.DoesNotContain(MenuEntry.Quit, MainMenu.Entries(false));
        Assert.Contains(MenuEntry.Quit, MainMenu.Entries(true));
    }

    /// <summary>Quit stays last: never under an Enter pressed out of reflex.</summary>
    [Fact]
    public void QuitIsTheLastEntry()
    {
        var entries = MainMenu.Entries(true);
        Assert.Equal(MenuEntry.Quit, entries[entries.Count - 1]);
    }

    [Fact]
    public void SouthMovesDownOneEntry()
    {
        int index;
        Assert.True(MainMenu.Move(0, 4, Direction.South, out index));
        Assert.Equal(1, index);
    }

    [Fact]
    public void NorthMovesUpOneEntry()
    {
        int index;
        Assert.True(MainMenu.Move(2, 4, Direction.North, out index));
        Assert.Equal(1, index);
    }

    /// <summary>
    /// Wrapping downwards. Without it, hammering the down arrow against the last entry produces
    /// nothing visible, and the menu has no rejection feedback to explain it.
    /// </summary>
    [Fact]
    public void FromTheLastEntrySouthWrapsToTheFirst()
    {
        int index;
        Assert.True(MainMenu.Move(3, 4, Direction.South, out index));
        Assert.Equal(0, index);
    }

    /// <summary>
    /// ⚠ Wrapping upwards is the case that breaks: <c>(0 - 1) % 4</c> is <b>-1</b> in C#, and a
    /// negative index names an entry that does not exist. This test is what holds the double modulo
    /// in <c>Move</c>.
    /// </summary>
    [Fact]
    public void FromTheFirstEntryNorthWrapsToTheLast()
    {
        int index;
        Assert.True(MainMenu.Move(0, 4, Direction.North, out index));
        Assert.Equal(3, index);
    }

    /// <summary>
    /// ⚠ East and West move nothing, and that is a decision: a snake player presses the side arrows
    /// out of reflex. Accepting them would jump the cursor at the very moment they are simply trying
    /// to turn.
    /// </summary>
    [Theory]
    [InlineData(Direction.East)]
    [InlineData(Direction.West)]
    public void SidewaysDirectionsMoveNothing(Direction direction)
    {
        int index;
        Assert.False(MainMenu.Move(1, 4, direction, out index));
        Assert.Equal(1, index);
    }

    /// <summary>
    /// The real clamping case: an index remembered from the desktop menu (4 entries) applied to the
    /// web menu (3 entries). Falling back to the last one beats throwing at the startup of a web
    /// build.
    /// </summary>
    [Fact]
    public void AnOutOfBoundsIndexFallsBackToTheLastEntry()
    {
        Assert.Equal(2, MainMenu.Clamp(3, 3));
        Assert.Equal(0, MainMenu.Clamp(-1, 3));
        Assert.Equal(1, MainMenu.Clamp(1, 3));
    }

    /// <summary>An empty menu is a composition defect, not a player input: it throws.</summary>
    [Fact]
    public void AMenuWithNoEntryThrows()
    {
        int index;
        Assert.Throws<ArgumentOutOfRangeException>(() => MainMenu.Move(0, 0, Direction.South, out index));
        Assert.Throws<ArgumentOutOfRangeException>(() => MainMenu.Clamp(0, 0));
    }
}
