using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Direct.RequestDevices
{
	[JsonConverter(typeof(RequestDevicesDataConverter))]
	public sealed class RequestDevicesData
	{
		public IEnumerable<int> Sources { get; set; }
		public IEnumerable<int> Destinations { get; set; }
	}

	public sealed class RequestDevicesDataConverter : AbstractGenericJsonConverter<RequestDevicesData>
	{
		private const string ATTR_SOURCES = "s";
		private const string ATTR_DESTINATIONS = "d";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, RequestDevicesData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Sources != null)
			{
				writer.WritePropertyName(ATTR_SOURCES);
				serializer.SerializeArray(writer, value.Sources);
			}

			if (value.Destinations != null)
			{
				writer.WritePropertyName(ATTR_DESTINATIONS);
				serializer.SerializeArray(writer, value.Destinations);
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, RequestDevicesData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SOURCES:
					instance.Sources = serializer.DeserializeArray<int>(reader);
					break;

				case ATTR_DESTINATIONS:
					instance.Destinations = serializer.DeserializeArray<int>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
