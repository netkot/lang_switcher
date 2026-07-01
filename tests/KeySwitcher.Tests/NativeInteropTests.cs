using System.Runtime.InteropServices;
using KeySwitcher.Interop;
using Xunit;

namespace KeySwitcher.Tests;

public class NativeInteropTests
{
    // SendInput передаёт Marshal.SizeOf<INPUT> как cbSize; если размер не совпадает с
    // ожидаемым системным (40 на x64, 28 на x86), SendInput падает с ошибкой 87.
    [Fact]
    public void InputStruct_MatchesNativeSize()
    {
        int expected = Environment.Is64BitProcess ? 40 : 28;
        Assert.Equal(expected, Marshal.SizeOf<NativeMethods.INPUT>());
    }
}
