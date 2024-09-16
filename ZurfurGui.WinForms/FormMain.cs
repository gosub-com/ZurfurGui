using System.Text.Json;
using ZurfurGui.Controls;

namespace ZurfurGui.WinForms;

public partial class FormMain : Form
{
    public FormMain()
    {
        InitializeComponent();
    }

    private void Form1_Load(object sender, EventArgs e)
    {

    }



    private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            var views = ZurfurGui.Controls.View.BuildViewTree(TestView.Test());

            views.Arrange(new Rect(300, 300, 200, 200));

        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }
}
