using System.Text.Encodings.Web;
using System.Text.Json;
using CC98.Objects;

namespace CC98.Services.Extensions;

public class JsonSerialize
{
    private static readonly JsonSerializerOptions LogOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        TypeInfoResolver = Cc98JsonContext.Default 
    };
    public static T? Deserialize<T>(string json)
    {
        try 
        {
            var obj = JsonSerializer.Deserialize<T>(json, LogOptions);
            return obj is T result ? result : default;
        }
        catch 
        {
            return default;
        }
    }
    public static string Serialize<T>(T obj)
    {
        try
        {
            return JsonSerializer.Serialize(obj, LogOptions);
        }
        catch
        {
            return string.Empty;
        }
    }
}