#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.API.Info;

namespace ICD.Connect.Krang.Remote.Direct.API
{
	[JsonConverter(typeof(ApiMessageDataConverter))]
	public sealed class ApiMessageData
	{
		public ApiClassInfo Command { get; set; }
		public bool IsResponse { get; set; }
	}

	public sealed class ApiMessageDataConverter : AbstractGenericJsonConverter<ApiMessageData>
	{
		private const string ATTR_COMMAND = "c";
		private const string ATTR_IS_RESPONSE = "r";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ApiMessageData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Command != null)
			{
				writer.WritePropertyName(ATTR_COMMAND);
				serializer.Serialize(writer, value.Command);
			}

			if (value.IsResponse)
				writer.WriteProperty(ATTR_IS_RESPONSE, value.IsResponse);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, ApiMessageData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_COMMAND:
					instance.Command = serializer.Deserialize<ApiClassInfo>(reader);
					break;

				case ATTR_IS_RESPONSE:
					instance.IsResponse = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
