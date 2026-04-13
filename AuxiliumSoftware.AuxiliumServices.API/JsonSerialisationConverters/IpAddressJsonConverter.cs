using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AuxiliumSoftware.AuxiliumServices.API.JsonSerialisationConverters
{
    public class IpAddressJsonConverter : JsonConverter<IPAddress>
    {
        public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var str = reader.GetString();
            return str is null ? null : IPAddress.Parse(str);
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
