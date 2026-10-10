using ComicBookReader.Core.Preferences;
using Xunit;

namespace ComicBookReader.Core.Tests.Preferences;

public sealed class ReadingPositionTests
{
    [Fact]
    public void PositionRestoresProportionallyWithDifferentScrollLimits()
    {
        var position = PreferencesResolver.CapturePosition("page", 3, 50, 300, 200, 400);
        Assert.Equal(new LastReadingPosition("page", 3, .25, .75), position);
        Assert.Equal((100d, 75d), PreferencesResolver.RestorePosition(position, 400, 100));
    }

    [Fact]
    public void AxisWithoutOverflowCapturesAndRestoresZero()
    {
        var position = PreferencesResolver.CapturePosition("page", 0, 50, 30, 0, 0);
        Assert.Equal(0, position.RelativeX);
        Assert.Equal(0, position.RelativeY);
        Assert.Equal((0d, 0d), PreferencesResolver.RestorePosition(
            new LastReadingPosition("page", 0, .5, 1), 0, 0));
    }

    [Fact]
    public void CaptureClampsOffsetsToScrollLimits()
    {
        var position = PreferencesResolver.CapturePosition("page", 0, -50, 500, 100, 200);
        Assert.Equal(0, position.RelativeX);
        Assert.Equal(1, position.RelativeY);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-.001)]
    [InlineData(1.001)]
    public void InvalidRelativePositionsAreRejected(double value)
    {
        foreach (var position in new[]
        {
            new LastReadingPosition("page", 0, value, .5),
            new LastReadingPosition("page", 0, .5, value)
        })
        {
            Assert.Throws<ArgumentException>(() => PreferencesResolver.RestorePosition(position, 100, 100));
            Assert.Throws<ArgumentException>(() => PreferencesResolver.Validate(new ReaderPreferences
            {
                Books = new() { ["book"] = new BookPreferences { Position = position } }
            }));
        }
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(-1)]
    public void InvalidScrollLimitsAreRejected(double limit)
    {
        var position = new LastReadingPosition("page", 0, .5, .5);
        Assert.Throws<ArgumentException>(() => PreferencesResolver.CapturePosition("page", 0, 0, 0, limit, 100));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.CapturePosition("page", 0, 0, 0, 100, limit));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.RestorePosition(position, limit, 100));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.RestorePosition(position, 100, limit));
    }

    [Fact]
    public void InvalidPageIndexAndNonFiniteOffsetsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => PreferencesResolver.CapturePosition("page", -1, 0, 0, 100, 100));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.CapturePosition("page", 0, double.NaN, 0, 100, 100));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.CapturePosition("page", 0, 0, double.PositiveInfinity, 100, 100));
        Assert.Throws<ArgumentException>(() => PreferencesResolver.RestorePosition(
            new LastReadingPosition("page", -1, 0, 0), 100, 100));
    }
}
