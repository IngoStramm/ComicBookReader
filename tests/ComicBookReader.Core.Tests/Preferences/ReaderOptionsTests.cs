using ComicBookReader.Core.Preferences;
using Xunit;

namespace ComicBookReader.Core.Tests.Preferences;

public sealed class ReaderOptionsTests
{
    [Fact]
    public void NewOptionsHaveCompatibleDefaults()
    {
        var profile = PreferencesResolver.Validate(new ReaderPreferences());
        Assert.Equal(90, profile.SpaceScrollPercent);
        Assert.Equal("pt-BR", profile.Language);
        Assert.Equal(PageClassificationOverride.Auto, profile.PageClassification);
        Assert.Equal("F12", profile.KeyBindings["ToggleToolbar"]);
        Assert.Equal("Ctrl+P", profile.KeyBindings["Preferences"]);
        Assert.Equal("R", profile.KeyBindings["ResetWidth"]);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(101)]
    public void SpaceDistanceOutsideItsRangeIsRejected(int value) =>
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences { SpaceScrollPercent = value }));

    [Theory]
    [InlineData("en")]
    [InlineData("pt-BR")]
    public void SupportedLanguagesAreAccepted(string language) =>
        Assert.Equal(language, PreferencesResolver.Validate(new ReaderPreferences { Language = language }).Language);

    [Theory]
    [InlineData("fr")]
    [InlineData("")]
    [InlineData(null)]
    public void UnsupportedLanguageIsRejected(string? language) =>
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences { Language = language! }));

    [Fact]
    public void InvalidGlobalClassificationIsRejected() =>
        Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences { PageClassification = (PageClassificationOverride)99 }));

    [Fact]
    public void AddedShortcutDefaultsDoNotInvalidateExistingCustomBindings()
    {
        var profile = PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new() { ["NextPage"] = "F12", ["Open"] = "Ctrl+P" }
        });
        Assert.Equal("F12", profile.KeyBindings["NextPage"]);
        Assert.Equal("Ctrl+P", profile.KeyBindings["Open"]);
        Assert.NotEqual("F12", profile.KeyBindings["ToggleToolbar"]);
        Assert.NotEqual("Ctrl+P", profile.KeyBindings["Preferences"]);
        Assert.Equal(profile.KeyBindings.Count, profile.KeyBindings.Values.Distinct().Count());
    }

    [Fact]
    public void DefaultWidthShortcutDoesNotReplaceAnOlderCustomBinding()
    {
        var profile = PreferencesResolver.Validate(new ReaderPreferences
        {
            KeyBindings = new() { ["Overview"] = "R", ["ZoomIn"] = "Add", ["ZoomOut"] = "Subtract" }
        });
        Assert.Equal("R", profile.KeyBindings["Overview"]);
        Assert.Equal("Add", profile.KeyBindings["ZoomIn"]);
        Assert.Equal("Subtract", profile.KeyBindings["ZoomOut"]);
        Assert.NotEqual("R", profile.KeyBindings["ResetWidth"]);
        Assert.Equal(profile.KeyBindings.Count, profile.KeyBindings.Values.Distinct().Count());
    }
}
