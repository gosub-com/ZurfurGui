using System.Text.Json;
using ZurfurGui.Controls;
using ZurfurGui.Render;
using ZurfurGui.WinForms.Interop;

namespace ZurfurGui.WinForms;

public partial class FormZurfurGui : Form
{
    WinWindow _window;
    Renderer _render;
    DateTime _showMagnification;

    double[] _mag = [0.25, 0.33, 0.5, 0.66, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4, 5];
    int _magIndex = 7;

    public FormZurfurGui(Properties controls)
    {
        InitializeComponent();
        _window = new WinWindow(this, pictureMain);
        _render = new Renderer(_window, controls);
        pictureMain.MouseWheel += PictureMain_MouseWheel;
    }

    private void PictureMain_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta < 0)
            _magIndex = Math.Max(0, _magIndex - 1);
        else
            _magIndex = Math.Min(_mag.Length - 1, _magIndex + 1);
        labelMag.Text = $"Magnification: {_mag[_magIndex] * 100:F0}%";
        labelMag.Visible = true;
        _showMagnification = DateTime.UtcNow;
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        pictureMain.Invalidate();
        labelMag.Visible = DateTime.UtcNow - _showMagnification < TimeSpan.FromSeconds(2);
    }

    private void pictureMain_Paint(object sender, PaintEventArgs e)
    {
        _window.DevicePixelRatio = _mag[_magIndex];

        // Hack the graphics into the context since Winforms requires us to use the supplied version
        ((WinContext)_window.PrimaryCanvas.Context)._graphics = e.Graphics;

        _render.RenderFrame();
    }
}
