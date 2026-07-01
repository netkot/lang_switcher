using System.Text.Json;
using KeySwitcher.Core;
using Xunit;

namespace KeySwitcher.Tests;

public class HotkeyTests
{
    [Fact]
    public void Matches_RequiresExactModifiers()
    {
        var shiftPause = new Hotkey(0x13, Ctrl: false, Shift: true, Alt: false);

        Assert.True(shiftPause.Matches(0x13, ctrl: false, shift: true, alt: false));
        Assert.False(shiftPause.Matches(0x13, ctrl: false, shift: false, alt: false)); // без Shift
        Assert.False(shiftPause.Matches(0x13, ctrl: true, shift: true, alt: false));   // лишний Ctrl
        Assert.False(shiftPause.Matches(0x20, ctrl: false, shift: true, alt: false));  // другой код
    }

    [Fact]
    public void None_NeverMatches()
    {
        Assert.False(Hotkey.None.IsSet);
        Assert.False(Hotkey.None.Matches(0, false, false, false));
    }

    [Fact]
    public void Describe_FormatsModifiersAndKey()
    {
        Assert.Equal("Pause", new Hotkey(0x13, false, false, false).Describe());
        Assert.Equal("Shift+Pause", new Hotkey(0x13, false, true, false).Describe());
        Assert.Equal("Ctrl+Alt+Pause", new Hotkey(0x13, true, false, true).Describe());
        Assert.Equal("—", Hotkey.None.Describe());
    }

    [Fact]
    public void JsonRoundTrip_PreservesHotkey()
    {
        var hk = new Hotkey(0x13, Ctrl: true, Shift: false, Alt: true);
        string json = JsonSerializer.Serialize(hk);
        var back = JsonSerializer.Deserialize<Hotkey>(json);
        Assert.Equal(hk, back);
    }
}
