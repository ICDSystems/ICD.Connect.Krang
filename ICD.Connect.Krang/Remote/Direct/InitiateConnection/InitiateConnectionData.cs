using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Direct.InitiateConnection
{
	[JsonConverter(typeof(InitiateConnectionDataConverter))]
	public sealed class InitiateConnectionData
	{
		public int DeviceId { get; set; }
	}

	public sealed class InitiateConnectionDataConverter : AbstractGenericJsonConverter<InitiateConnectionData>
	{
		private const string ATTR_DEVICE_ID = "d";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, InitiateConnectionData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.DeviceId != 0)
				writer.WriteProperty(ATTR_DEVICE_ID, value.DeviceId);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, InitiateConnectionData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_DEVICE_ID:
					instance.DeviceId = reader.GetValueAsInt();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
