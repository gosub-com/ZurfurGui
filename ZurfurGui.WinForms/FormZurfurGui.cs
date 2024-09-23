using System.Text.Json;
using ZurfurGui.Controls;
using ZurfurGui.Render;
using ZurfurGui.WinForms.Interop;

namespace ZurfurGui.WinForms;

public partial class FormZurfurGui : Form
{
    WinGlobal _global;
    Renderer _render;

    double[] _mag = [0.25, 0.33, 0.5, 0.66, 0.75, 0.8, 0.9, 1, 1.1, 1.25, 1.5, 1.75, 2, 2.5, 3, 4, 5];
    int _magIndex = 7;

    public FormZurfurGui()
    {
        InitializeComponent();
        _global = new WinGlobal(this, pictureMain);
        _render = new Renderer();
        
        pictureMain.MouseWheel += PictureMain_MouseWheel;
    }

    private void PictureMain_MouseWheel(object? sender, MouseEventArgs e)
    {
        if (e.Delta < 0)
            _magIndex = Math.Max(0, _magIndex - 1);
        else
            _magIndex = Math.Min(_mag.Length - 1, _magIndex + 1);
        labelMag.Text = $"Magnification: {_mag[_magIndex] * 100:F0}%";
    }

    private void FormZurfurGui_Load(object sender, EventArgs e)
    {
    }



    private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            var views = ZurfurGui.Controls.View.BuildViewTree(TestView.Test());

            views.Measure(new Size(200, 200));
            views.Arrange(new Rect(300, 300, 200, 200));

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        pictureMain.Invalidate();
    }

    private void pictureMain_Paint(object sender, PaintEventArgs e)
    {
        var context = _global.PrimaryCanvas.Context;
        context.PixelScale = _mag[_magIndex];

        // Hack the graphics into the context since Winforms requires us to use the supplied version
        ((WinContext)context)._graphics = e.Graphics;

        _render.RenderFrame(_global);
    }
}
