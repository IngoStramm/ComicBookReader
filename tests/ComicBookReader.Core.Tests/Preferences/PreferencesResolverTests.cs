using ComicBookReader.Core.Preferences;
using Xunit;

namespace ComicBookReader.Core.Tests.Preferences;

public sealed class PreferencesResolverTests
{
    [Fact]
    public void EmptyProfileUsesDefaults()
    {
        var preferences = PreferencesResolver.Validate(new ReaderPreferences());
        Assert.Equal(1, preferences.SchemaVersion);
        Assert.Equal(1, PreferencesResolver.ResolveWidth(preferences, "new-book"));
        Assert.Equal(.6, preferences.ScrollSpeed);
        Assert.True(preferences.LastFullscreen);
        Assert.Equal("PageDown", preferences.KeyBindings["NextPage"]);
        Assert.Equal("PageUp", preferences.KeyBindings["PreviousPage"]);
        Assert.Equal("V", preferences.KeyBindings["Overview"]);
        Assert.Equal("F11", preferences.KeyBindings["Fullscreen"]);
        Assert.Equal("Ctrl+O", preferences.KeyBindings["Open"]);
        Assert.Equal("OemPlus", preferences.KeyBindings["ZoomIn"]);
        Assert.Equal("OemMinus", preferences.KeyBindings["ZoomOut"]);
        Assert.Equal(PageClassificationOverride.Auto,
            PreferencesResolver.ResolveClassification(preferences, "new-book", "page"));
    }

    [Fact]
    public void BookOverridesOnlyItsOwnWidthAndPageClassification()
    {
        var preferences = new ReaderPreferences
        {
            PreferredPageWidthRatio = 1.4,
            Books = new()
            {
                ["book"] = new BookPreferences
                {
                    WidthRatio = .8,
                    Classifications = new() { ["page"] = PageClassificationOverride.Spread }
                }
            }
        };
        Assert.Equal(.8, PreferencesResolver.ResolveWidth(preferences, "book"));
        Assert.Equal(1.4, PreferencesResolver.ResolveWidth(preferences, "other-book"));
        Assert.Equal(PageClassificationOverride.Spread,
            PreferencesResolver.ResolveClassification(preferences, "book", "page"));
        Assert.Equal(PageClassificationOverride.Auto,
            PreferencesResolver.ResolveClassification(preferences, "book", "other-page"));
        Assert.Equal(PageClassificationOverride.Auto,
            PreferencesResolver.ResolveClassification(preferences, "other-book", "page"));
    }

    [Fact]
    public void AbsentBookWidthUsesGlobalAndOpaqueIdentitiesRemainCaseSensitive()
    {
        var preferences = new ReaderPreferences
        {
            PreferredPageWidthRatio = 1.5,
            Books = new()
            {
                ["BOOK"] = new BookPreferences(),
                ["book"] = new BookPreferences { WidthRatio = .5 }
            }
        };
        Assert.Equal(1.5, PreferencesResolver.ResolveWidth(preferences, "BOOK"));
        Assert.Equal(.5, PreferencesResolver.ResolveWidth(preferences, "book"));
    }

    [Theory]
    [InlineData(.25, .1)]
    [InlineData(2, 2)]
    public void NumericRangeEndpointsAreAccepted(double width, double speed)
    {
        var preferences = PreferencesResolver.Validate(new ReaderPreferences
        {
            PreferredPageWidthRatio = width, ScrollSpeed = speed,
            Books = new() { ["book"] = new BookPreferences { WidthRatio = width } }
        });
        Assert.Equal(width, preferences.PreferredPageWidthRatio);
        Assert.Equal(speed, preferences.ScrollSpeed);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(.249)]
    [InlineData(2.001)]
    public void InvalidGlobalAndBookWidthsAreRejected(double value)
    {
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(
            new ReaderPreferences { PreferredPageWidthRatio = value }));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            Books = new() { ["book"] = new BookPreferences { WidthRatio = value } }
        }));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(.099)]
    [InlineData(2.001)]
    public void InvalidSpeedIsRejected(double value) => Assert.Throws<ArgumentException>(
        () => PreferencesResolver.Validate(new ReaderPreferences { ScrollSpeed = value }));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2)]
    public void UnsupportedSchemaIsRejectedExplicitly(int version)
    {
        var exception = Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(
            new ReaderPreferences { SchemaVersion = version }));
        Assert.Contains("SchemaVersion", exception.Message);
    }

    [Fact]
    public void PartialBindingsReceiveDefaultsAndNormalizeCaseAndModifierOrder()
    {
        var preferences = PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new() { ["nextpage"] = " shift + CTRL + n " }
        });
        Assert.Equal("Ctrl+Shift+N", preferences.KeyBindings["NextPage"]);
        Assert.Equal("PageUp", preferences.KeyBindings["PreviousPage"]);
        Assert.Equal(7, preferences.KeyBindings.Count);
    }

    [Theory]
    [InlineData("v")]
    [InlineData("pageup")]
    [InlineData("Up")]
    [InlineData("Ctrl+Down")]
    [InlineData("Left")]
    [InlineData("Right")]
    [InlineData("Escape")]
    [InlineData("Ctrl+Ctrl+N")]
    [InlineData("Ctrl+")]
    [InlineData("Win+N")]
    [InlineData("UnknownKey")]
    [InlineData("F25")]
    [InlineData("")]
    public void ConflictingReservedOrMalformedBindingsAreRejected(string binding)
    {
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new() { ["NextPage"] = binding }
        }));
    }

    [Fact]
    public void EquivalentModifierOrdersConflict()
    {
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new()
            {
                ["NextPage"] = "Shift+Ctrl+N", ["PreviousPage"] = "ctrl+shift+n"
            }
        }));
    }

    [Theory]
    [InlineData("alt+f24", "Alt+F24")]
    [InlineData("ctrl+d2", "Ctrl+D2")]
    [InlineData("NumPad0", "NumPad0")]
    [InlineData("shift+home", "Shift+Home")]
    public void KnownKeysAreAcceptedWithoutWpfDependency(string input, string expected)
    {
        var preferences = PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new() { ["NextPage"] = input }
        });
        Assert.Equal(expected, preferences.KeyBindings["NextPage"]);
    }

    [Fact]
    public void NullCollectionsAndBookRecordsAreRejectedExplicitly()
    {
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(
            new ReaderPreferences { KeyBindings = null! }));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(
            new ReaderPreferences { Books = null! }));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            Books = new() { ["book"] = null! }
        }));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            Books = new() { ["book"] = new BookPreferences { Classifications = null! } }
        }));
    }

    [Fact]
    public void UnknownAndRepeatedCommandNamesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new() { ["Unknown"] = "N" }
        }));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new() { ["NextPage"] = "N", ["nextpage"] = "M" }
        }));
    }

    [Fact]
    public void InvalidClassificationIsRejected()
    {
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
        {
            Books = new()
            {
                ["book"] = new BookPreferences
                {
                    Classifications = new() { ["page"] = (PageClassificationOverride)99 }
                }
            }
        }));
    }

    [Fact]
    public void ValidatedSnapshotIsDetachedFromInputCollections()
    {
        var input = new ReaderPreferences
        {
            Books = new()
            {
                ["book"] = new BookPreferences
                {
                    WidthRatio = .8,
                    Classifications = new() { ["page"] = PageClassificationOverride.Single }
                }
            }
        };
        var snapshot = PreferencesResolver.Validate(input);
        input.KeyBindings["NextPage"] = "N";
        input.Books["book"].Classifications["page"] = PageClassificationOverride.Spread;
        input.Books.Clear();
        Assert.Equal("PageDown", snapshot.KeyBindings["NextPage"]);
        Assert.Equal(.8, PreferencesResolver.ResolveWidth(snapshot, "book"));
        Assert.Equal(PageClassificationOverride.Single,
            PreferencesResolver.ResolveClassification(snapshot, "book", "page"));
    }
}
