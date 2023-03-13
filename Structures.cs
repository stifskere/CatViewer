
using Newtonsoft.Json;
using JetBrains.Annotations;
using System.Runtime.InteropServices;
using System.Windows;

namespace CatViewer;

[JsonObject(MemberSerialization.OptIn), PublicAPI]
public struct CatApiEntry
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("url")] public string Url { get; set; }
    [JsonProperty("width")] public int Width { get; set; }
    [JsonProperty("height")] public int Height { get; set; }
}

[JsonObject(MemberSerialization.OptIn), PublicAPI]
public struct Breed
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }

    public override string ToString()
        => Name;
}

[JsonObject(MemberSerialization.OptIn), PublicAPI]
public struct Vote
{
    [JsonProperty("image_id")] public string ImageId { get; set; }
    [JsonProperty("sub_id")] public string SubId { get; set; }
    [JsonProperty("value")] public bool Score { get; set; }

    public Vote(string imageId, string subId, bool score)
    {
        ImageId = imageId;
        SubId = subId;
        Score = score;
    }
}

public struct HttpHeader
{
    public HttpHeader(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }
    public string Value { get; }
}

public static class Mouse {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    // ReSharper disable once MemberCanBePrivate.Global
    internal static extern bool GetCursorPos(ref Win32Point pt);

    internal struct Win32Point
    {
        public int X;
        public int Y;
    }

    public static Point GetMousePosition()
    {
        Win32Point pt = new();
        GetCursorPos(ref pt);
        return new Point(pt.X, pt.Y);
    }
}