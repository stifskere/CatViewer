
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Windows;

namespace CatViewer.Windows;

public partial class Console
{
    private static readonly List<string> Lines = new();

    public Console()
    {
        InitializeComponent();
        ConsoleBox.Text = string.Join('\n', Lines);
    }

    [PublicAPI]
    public void WriteLine(params object[] text)
    {
        foreach (object item in text)
            Lines.Add(item?.ToString() ?? "<null>");

        ConsoleBox.Text = string.Join('\n', Lines);
    }

    private void ClearConsole(object sender, RoutedEventArgs e)
    {
        Lines.Clear();
        ConsoleBox.Text = string.Empty;
    }
}