using System.Text;
using System.IO;
using NativeElements.Helpers;

namespace NativeElements;

public partial class DeveloperPage : ContentPage
{
    public DeveloperPage()
    {
        InitializeComponent();
    }

    private async void OnRunTestsClicked(object? sender, EventArgs e)
    {
        TestOutputLabel.Text = "Running tests...";
        await Task.Delay(50);

        var sb = new StringBuilder();
        using (var sw = new StringWriter(sb))
        {
            var originalOut = Console.Out;
            try
            {
                Console.SetOut(sw);
                await Task.Run(() => CushionUnitTestRunner.RunAllTests());
                sw.Flush();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
        TestOutputLabel.Text = sb.ToString();
    }
}
