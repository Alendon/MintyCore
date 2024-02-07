
using AssetManagementBase;
using FontStashSharp;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace TestMod;

public class TestUiWindow : Window
{
    public TestUiWindow()
    {
        var label1 = new Label();
        label1.Text = "Positioned Text";
        label1.Left = 50;
        label1.Top = 100;

        var textButton1 = new TextBox();
        textButton1.Text = "Padded Centered Button";
        textButton1.HorizontalAlignment = HorizontalAlignment.Center;
        textButton1.VerticalAlignment = VerticalAlignment.Center;

        var label2 = new Label();
        label2.Text = "Right Bottom Text";
        label2.Left = -30;
        label2.Top = -20;
        label2.HorizontalAlignment = HorizontalAlignment.Right;
        label2.VerticalAlignment = VerticalAlignment.Bottom;

        var textButton2 = new TextButton();
        textButton2.Text = "Fixed Size Button";
        textButton2.Width = 110;
        textButton2.Height = 80;

        var panel1 = new Panel();
        panel1.Widgets.Add(label1);
        panel1.Widgets.Add(textButton1);
        panel1.Widgets.Add(label2);
        panel1.Widgets.Add(textButton2);

        var color = FSColor.Aquamarine;
        color.A = 128;
        Background = new SolidBrush(color);

			
        Title = "Hello World";
        Left = 547;
        Top = 269;
        MinWidth = 500;
        MinHeight = 200;
        ZIndex = -2;
        Content = panel1;

        CloseButton.Enabled = false;
        CloseButton.Visible = false;
    }
}