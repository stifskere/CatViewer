
global using static CatViewer.Methods;

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;

namespace CatViewer;

public static class Methods
{
    private static void ClearAndSetHeaders(params HttpHeader[] headers)
    {
        ProgramWindow.Client.DefaultRequestHeaders.Clear();

        foreach (HttpHeader header in headers)
            ProgramWindow.Client.DefaultRequestHeaders.TryAddWithoutValidation(header.Name, header.Value);
    }

    private static async void ShowErrorMessage(HttpResponseMessage response)
        => MessageBox.Show($"There was an error on the get request: {response.StatusCode}\nApi response: {await response.Content.ReadAsStringAsync()}", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);


    public static async Task<HttpContent?> HttpRequestAsync(string url, params HttpHeader[] headers)
    {
        ClearAndSetHeaders(headers);

        HttpResponseMessage response = await ProgramWindow.Client.GetAsync(url);
        ProgramWindow.ProgramConsole.WriteLine($"GET {response.StatusCode}: {url}");

        if (response.IsSuccessStatusCode)
            return response.Content;

        ShowErrorMessage(response);

        return null;
    }

    public static async Task<bool> HttpPostAsync(string url, HttpContent content, params HttpHeader[] headers)
    {
        ClearAndSetHeaders(headers);
        HttpResponseMessage response = await ProgramWindow.Client.PostAsync(url, content);

        ProgramWindow.ProgramConsole.WriteLine($"POST {response.StatusCode}: {url}");
        ProgramWindow.ProgramConsole.WriteLine(response.RequestMessage!.ToString());

        if (!response.IsSuccessStatusCode)
            ShowErrorMessage(response);

        return response.IsSuccessStatusCode;
    }

    public static BitmapImage GetBitmapImageFromStream(ref Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = stream;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        return bitmapImage;
    }
}