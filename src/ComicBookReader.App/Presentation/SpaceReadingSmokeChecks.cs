using System.Diagnostics;
using System.Windows.Input;
using ComicBookReader.Core.Preferences;
using ComicBookReader.Core.Reading;

namespace ComicBookReader.App.Presentation;

internal static class SpaceReadingSmokeChecks
{
    public static async Task RunAsync(MainWindow window)
    {
        var reader = window.Reader;
        foreach (var direction in new[] { ReadingDirection.LeftToRight, ReadingDirection.RightToLeft })
        {
            window.ApplyUserPreferences(reader.CapturePreferences() with { ReadingDirection = direction, SpaceScrollPercent = 90 });
            await reader.NavigateAsync(-1);
            var layout = reader.State.Layout!;
            Require(layout.MaxOffsetXDip > 0 && layout.MaxOffsetYDip > 0, "Space test requires overflow on both axes.");
            bool rightToLeft = direction == ReadingDirection.RightToLeft;
            double start = rightToLeft ? layout.MaxOffsetXDip : 0;
            double end = rightToLeft ? 0 : layout.MaxOffsetXDip;
            Require(reader.State.OffsetXDip == start && reader.State.OffsetYDip == 0, "New page did not start at the reading edge.");
            int horizontalMoves = 0, rows = 0;
            bool advanced = false;
            for (int press = 0; press < 100; press++)
            {
                var previous = reader.State;
                ReadingSmokeChecks.RaiseKey(window.Viewport, Key.Space, true);
                var timer = Stopwatch.StartNew();
                while ((reader.State == previous || reader.State.Mode != ReaderMode.Reading) && timer.Elapsed.TotalSeconds < 3)
                    await Task.Delay(10);
                var current = reader.State;
                Require(current.Mode == ReaderMode.Reading && current != previous, "Space command did not finish.");
                if (current.PresentedIndex != previous.PresentedIndex)
                {
                    Require(previous.OffsetXDip == end && previous.OffsetYDip == layout.MaxOffsetYDip,
                        "Space skipped an unread section before advancing the page.");
                    Require(current.PresentedIndex == previous.PresentedIndex + 1 && current.OffsetYDip == 0 &&
                        current.OffsetXDip == (rightToLeft ? current.Layout!.MaxOffsetXDip : 0),
                        "Space did not advance to the top reading edge of the next page.");
                    advanced = true;
                    break;
                }
                if (current.OffsetYDip == previous.OffsetYDip)
                {
                    double distance = rightToLeft ? previous.OffsetXDip - current.OffsetXDip : current.OffsetXDip - previous.OffsetXDip;
                    Require(distance > 0 && distance <= window.Viewport.ActualWidth * .9 + .001,
                        "Space moved horizontally in the wrong direction or skipped visible content.");
                    horizontalMoves++;
                }
                else
                {
                    Require(previous.OffsetXDip == end && current.OffsetXDip == start && current.OffsetYDip > previous.OffsetYDip &&
                        current.OffsetYDip - previous.OffsetYDip <= window.Viewport.ActualHeight * .9 + .001,
                        "Space did not finish the row and return to the start of the next row.");
                    rows++;
                }
            }
            Require(advanced && horizontalMoves >= 2 && rows > 0, "Space did not traverse a complete page in the selected direction.");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
