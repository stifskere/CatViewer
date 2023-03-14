
global using static CatViewer.Windows.MainWindow;

using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Windows;
using System.Reflection;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Windows.Controls;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CatViewer.Windows;

[UsedImplicitly]
public partial class MainWindow
{
    #region fields and properties
    private const string CatApiKey = "live_nYqGRnWMqiHgwgV4c8MJgZNydEXTRMRw1TEP4SZMzjpTkCB1ZjTihHqyDBW9tcpR";

    private Stream? _currentImage = new MemoryStream();
    private List<CatApiEntry> _currentCats = new(); 
    private string _currentImageId = string.Empty;
    private string? _userId;
    private bool? _currentVote;

    public Console ProgramConsole = new();
    public readonly HttpClient Client = new();

    public static MainWindow ProgramWindow { get; private set; } = null!;
    #endregion

    #region ctor and destructor
    public MainWindow()
    {
        InitializeComponent();
        ProgramWindow = this;
        AsyncCtr();
        Closed += OnWindowClosing;
    }

    private async void AsyncCtr()
    {
        HttpContent? allBreeds = await HttpRequestAsync("https://api.thecatapi.com/v1/breeds", true, "Connection Error: There was an error while connection to the api due to a network problem, the app will close.", new HttpHeader("x-api-key", CatApiKey));

        Breed[] breeds = JsonConvert.DeserializeObject<Breed[]>(await allBreeds!.ReadAsStringAsync())!;

        BreedsBox.Items.Add(new Breed{Id = "Any", Name = "Any"});
        BreedsBox.SelectedIndex = 0;
        foreach (Breed breed in breeds)
            BreedsBox.Items.Add(breed);
    }

    ~MainWindow()
    {
        Client.Dispose();
        _currentImage?.Dispose();
    }
    #endregion

    #region non static methods
    private void ChangeUserId() 
        => _userId ??= "CatViewer-" + ((from nic in NetworkInterface.GetAllNetworkInterfaces() where nic.OperationalStatus == OperationalStatus.Up select nic.GetPhysicalAddress().ToString()).FirstOrDefault() ?? "Temporary user");
    #endregion

    #region events
    private void OnWindowClosing(object? sender, EventArgs eventArgs)
        => Environment.Exit(0);

    private void WindowMouseMoved(object sender, MouseEventArgs mouseEventArgs) 
        => DragMove();

    private void OnWindowMinimized(object sender, EventArgs eventArgs)
        => WindowState = WindowState.Minimized;

    // TODO save positions and make the full screen fit
    private void OnWindowResize(object? sender, EventArgs eventArgs)
    {
        if (WindowState is WindowState.Normal or WindowState.Minimized)
        {
            Left = SystemParameters.WorkArea.Left;
            Top = SystemParameters.WorkArea.Top;
            Width = SystemParameters.WorkArea.Width;
            Height = SystemParameters.WorkArea.Height;
        }
        else WindowState = WindowState.Normal;
    }

    private void OnWindowDragDelta(object sender, DragDeltaEventArgs e)
    {
        if (sender is not Thumb thumb) 
            return;
        
        switch (thumb.Name)
        {
            case "TopResizeThumb":
            {
                double newHeight = Math.Max(MinHeight, Height - e.VerticalChange);
                Top += Height - newHeight;
                Height = newHeight;
            }
                break;
            case "BottomResizeThumb":
            {
                Height = Math.Max(MinHeight, Height + e.VerticalChange);
            }
                break;
            case "LeftResizeThumb":
            {
                double newWidth = Math.Max(MinWidth, Width - e.HorizontalChange);
                Left += Width - newWidth;
                Width = newWidth;
            }
                break;
            case "RightResizeThumb":
            {
                Width = Math.Max(MinWidth, Width + e.HorizontalChange);
            }
                break;
            case "TopLeftResizeThumb":
            {
                double newWidth = Math.Max(MinWidth, Width - e.HorizontalChange);
                double newHeight = Math.Max(MinHeight, Height - e.VerticalChange);
                Left += Width - newWidth;
                Top += Height - newHeight;
                Width = newWidth;
                Height = newHeight;
            }
                break;
            case "TopRightResizeThumb":
            {
                double newWidth = Math.Max(MinWidth, Width + e.HorizontalChange);
                double newHeight = Math.Max(MinHeight, Height - e.VerticalChange);
                Top += Height - newHeight;
                Width = newWidth;
                Height = newHeight;
            }
                break;
            case "BottomLeftResizeThumb":
            {
                double newWidth = Math.Max(MinWidth, Width - e.HorizontalChange);
                double newHeight = Math.Max(MinHeight, Height + e.VerticalChange);
                Left += Width - newWidth;
                Width = newWidth;
                Height = newHeight;
            }
                break;
            case "BottomRightResizeThumb":
            {
                Width = Math.Max(MinWidth, Width + e.HorizontalChange);
                Height = Math.Max(MinHeight, Height + e.VerticalChange);
            }
                break; 
        }
    }

    private async void GetCatButton(object sender, RoutedEventArgs routedEventArgs)
    {
        if (_currentCats.Count == 0)
        {
            Breed currentBreed = (Breed)BreedsBox.SelectedItem;
            using HttpContent? catEntryResponse = await HttpRequestAsync($"https://api.thecatapi.com/v1/images/search?limit=10{(currentBreed.Name.ToLower() != "any" ? $"&breed_ids={currentBreed.Id}" : "")}", new HttpHeader("x-api-key", CatApiKey));

            if (catEntryResponse == null)
                return;

            _currentCats = JsonConvert.DeserializeObject<CatApiEntry[]>(await catEntryResponse.ReadAsStringAsync())!.ToList();
        }

        _currentImageId = _currentCats[0].Id;
        using HttpContent? catImageResponse = await HttpRequestAsync(_currentCats[0].Url);

        _currentCats.RemoveAt(0);

        if (catImageResponse == null)
            return;

        _currentImage = new MemoryStream(await catImageResponse.ReadAsByteArrayAsync(), true);

        ImageDisplay.Source = GetBitmapImageFromStream(ref _currentImage);

        DownloadItem.IsEnabled = true;
        DownVoteButton.IsEnabled = true;
        UpVoteButton.IsEnabled= true;
        VoteLabel.Content = "No vote issued";
        _currentVote = null;
    }

    private void SaveCatButton(object sender, RoutedEventArgs routedEventArgs)
    {
        if (_currentImage is not { CanSeek: true })
        {
            MessageBox.Show("Couldn't save the cat because there is nothing on the current stream or it can't be read.", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            return;
        }

        SaveFileDialog saveFileDialog = new()
        {
            Title = "Save a cat image",
            AddExtension = true,
            Filter = "Portable network graphics (*.png)|*.png|Joint Photographic Experts Group (*.jpg)|*.jpg",
            CheckPathExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (saveFileDialog.ShowDialog() != true) return;

        using FileStream file = File.Create(saveFileDialog.FileName);
        _currentImage.Seek(0, SeekOrigin.Begin);
        _currentImage.CopyTo(file);
    }

    private void CopyCatButton(object sender, RoutedEventArgs routedEventArgs)
    {
        Clipboard.SetImage(GetBitmapImageFromStream(ref _currentImage!));
    }

    private void ShowConsole(object sender, RoutedEventArgs routedEventArgs)
    {
        PropertyInfo isDisposedPropertyClass = typeof(Console).GetProperty("IsDisposed", BindingFlags.Instance | BindingFlags.NonPublic)!;
        bool isWindowDisposed = (bool)(isDisposedPropertyClass.GetValue(ProgramConsole) ?? true);

        if (isWindowDisposed)
            ProgramConsole = new Console();

        ProgramConsole.Show();
    }

    private void BreedsChanged(object sender, SelectionChangedEventArgs e)
        => _currentCats.Clear();
    

    private async void VoteChanged(object sender, RoutedEventArgs routedEventArgs)
    {
        Button touchedButton = (Button)sender;

        bool currentVote = touchedButton.Name == "UpVoteButton";

        if (currentVote == _currentVote)
            return;

        _currentVote = currentVote;

        string voteStringResult = _currentVote.Value ? "Voted positively" : "Voted negatively";

        ChangeUserId();
        string voteContent = JsonConvert.SerializeObject(new Vote(_currentImageId, _userId!, _currentVote.Value));
        bool postResult = await HttpPostAsync("https://api.thecatapi.com/v1/votes", new StringContent(voteContent, Encoding.UTF8, "application/json"), new HttpHeader("x-api-key", CatApiKey));

        if (!postResult)
        {
            ProgramConsole.WriteLine("VOTES: Failed for reason specified above");
            VoteLabel.Content = "Failed to vote";
            return;
        }

        ProgramConsole.WriteLine($"VOTES: {voteStringResult}");
        VoteLabel.Content = voteStringResult;
    }

    private async void UploadImage(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Title = "Upload a cat image",
            AddExtension = true,
            Filter = "Portable network graphics (*.png)|*.png|Joint Photographic Experts Group (*.jpg)|*.jpg",
            CheckPathExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (dialog.ShowDialog() != true) 
            return;

        await using Stream fileStream = File.OpenRead(dialog.FileName);
        //byte[] data = new byte[fileStream.Length];
        //_ = await fileStream.ReadAsync(data, CancellationToken.None);

        ChangeUserId();

        using MultipartFormDataContent content = new(Guid.NewGuid().ToString()) { HeaderEncodingSelector = null };
        StreamContent fileContent = new(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        content.Add(new StreamContent(fileStream), "file", Regex.Split(dialog.FileName, "[\\\\/]").Last());
        content.Add(new StringContent(_userId!), "sub_id");

        bool result = await HttpPostAsync("https://api.thecatapi.com/v1/images/upload", content, new HttpHeader("x-api-key", CatApiKey));

        if (result)
            MessageBox.Show("Uploaded successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK);
    }
    #endregion
}