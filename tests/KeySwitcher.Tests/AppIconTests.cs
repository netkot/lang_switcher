using System.Drawing;
using System.Windows.Forms;
using Xunit;

namespace KeySwitcher.Tests;

public class AppIconTests
{
    [Fact]
    public void EmbeddedAppIcon_DecodesAtSmallSize()
    {
        var asm = typeof(KeySwitcher.App.TrayAppContext).Assembly;
        using Stream? s = asm.GetManifestResourceStream("KeySwitcher.app.ico");
        Assert.NotNull(s);

        // Тот же путь, что и в трее: выбираем кадр под размер иконки области уведомлений.
        using var icon = new Icon(s!, SystemInformation.SmallIconSize);
        using Bitmap bmp = icon.ToBitmap();

        Assert.True(bmp.Width > 0 && bmp.Height > 0);

        // Кадр должен реально декодироваться (не пустой прозрачный квадрат).
        bool hasOpaquePixel = false;
        for (int y = 0; y < bmp.Height && !hasOpaquePixel; y++)
            for (int x = 0; x < bmp.Width; x++)
                if (bmp.GetPixel(x, y).A > 0) { hasOpaquePixel = true; break; }

        Assert.True(hasOpaquePixel, "иконка декодировалась как полностью прозрачная");
    }
}
