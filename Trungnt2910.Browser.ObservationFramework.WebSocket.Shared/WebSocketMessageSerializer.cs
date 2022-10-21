using Newtonsoft.Json;
using System.Text;
using Trungnt2910.Browser.ObservationFramework.WebSocket.Messages;

namespace Trungnt2910.Browser.ObservationFramework.WebSocket;

internal static class WebSocketMessageSerializer
{
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
    {
        TypeNameHandling= TypeNameHandling.All
    };

    public static byte[] Serialize<T>(T message) where T: MessageData
    {
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, JsonSerializerSettings));

        return bytes;
    }

    public static T? Deserialize<T>(byte[] bytes) where T: MessageData
    {
        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes), JsonSerializerSettings);
    }

    public static T? Deserialize<T>(string str) where T : MessageData
    {
        return JsonConvert.DeserializeObject<T>(str, JsonSerializerSettings);
    }
}
