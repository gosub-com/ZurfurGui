using System.Diagnostics;
using System.Drawing.Imaging;
using ZurfurGui.Render;
using ZurfurGui.Controls;
using ZurfurGui.WinForms.Interop;

namespace ZurfurGui.WinForms;

public partial class FormZurfurGui : Form
{
    WinWindow _window;
    WinCanvas _canvas;
    Renderer _render;
    Bitmap _bitmap = new(1, 1, PixelFormat.Format32bppRgb);
    DateTime _showInfo;

    double[] _mag = [0.25, 0.33, 0.5, 0.66, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4, 5];
    int _magIndex = 7;

    public FormZurfurGui(Action<AppWindow> mainAppEntry)
    {
        InitializeComponent();
        var appWindow = Loader.Init(mainAppEntry);
        _window = new WinWindow(this, pictureMain);
        _canvas = new WinCanvas(pictureMain);
        _render = new Renderer(_window, _canvas, appWindow);
        pictureMain.MouseWheel += PictureMain_MouseWheel;
    }

    private void PictureMain_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta < 0)
            _magIndex = Math.Max(0, _magIndex - 1);
        else
            _magIndex = Math.Min(_mag.Length - 1, _magIndex + 1);
        labelInfo.Text = $"Magnification: {_mag[_magIndex] * 100:F0}%";
        labelInfo.Visible = true;
        _showInfo = DateTime.UtcNow;
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        try
        {
            // Update bitmap size
            if (_bitmap.Size != pictureMain.Size)
                _bitmap = new Bitmap(pictureMain.Width, pictureMain.Height, PixelFormat.Format32bppRgb);

            using var gr = Graphics.FromImage(_bitmap);
            _window.DevicePixelRatio = _mag[_magIndex];

            // Hack the graphics into the context since Winforms requires us to use the supplied version
            ((WinContext)_canvas.Context)._graphics = gr;

            _render.RenderFrame();
            pictureMain.Image = _bitmap;
            labelInfo.Visible = DateTime.UtcNow - _showInfo < TimeSpan.FromSeconds(2);
        }
        catch (Exception ex)
        {
            Debug.Assert(false);
            _showInfo = DateTime.UtcNow;
            labelInfo.Text = $"ERROR: {ex.Message}";
            labelInfo.Visible = true;
            Console.WriteLine($"Error during rendering: {ex.Message}");
        }
    }
}
