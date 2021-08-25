#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Endpoints.Destinations;
using ICD.Connect.Routing.Endpoints.Sources;

namespace ICD.Connect.Krang.Remote.Direct.ShareDevices
{
	[JsonConverter(typeof(ShareDevicesDataConverter))]
	public sealed class ShareDevicesData
	{
		public IEnumerable<ISource> Sources { get; set; }
		public IEnumerable<IDestination> Destinations { get; set; }
		public Dictionary<int, IEnumerable<ConnectorInfo>> SourceConnections { get; set; }
		public Dictionary<int, IEnumerable<ConnectorInfo>> DestinationConnections { get; set; }
	}

	public sealed class ShareDevicesDataConverter : AbstractGenericJsonConverter<ShareDevicesData>
	{
		private const string ATTR_SOURCES = "s";
		private const string ATTR_DESTINATIONS = "d";
		private const string ATTR_SOURCE_CONNECTIONS = "sc";
		private const string ATTR_DESTINATION_CONNECTIONS = "dc";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ShareDevicesData value, JsonSerializer serializer)
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

			if (value.SourceConnections != null)
			{
				writer.WritePropertyName(ATTR_SOURCE_CONNECTIONS);
				serializer.SerializeDict(writer, value.SourceConnections, (s, w, k) => w.WriteValue(k), (s, w, v) => s.SerializeArray(w, v));
			}

			if (value.DestinationConnections != null)
			{
				writer.WritePropertyName(ATTR_DESTINATION_CONNECTIONS);
				serializer.SerializeDict(writer, value.DestinationConnections, (s, w, k) => w.WriteValue(k),
				                         (s, w, v) => s.SerializeArray(w, v));
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, ShareDevicesData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SOURCES:
					instance.Sources = serializer.DeserializeArray<ISource>(reader);
					break;

				case ATTR_DESTINATIONS:
					instance.Destinations = serializer.DeserializeArray<IDestination>(reader);
					break;

				case ATTR_SOURCE_CONNECTIONS:
					instance.SourceConnections =
						serializer.DeserializeDict(reader, (s, r) => r.GetValueAsInt(),
						                                                            (s, r) => s.DeserializeArray<ConnectorInfo>(r))
						          .ToDictionary();
					break;

				case ATTR_DESTINATION_CONNECTIONS:
					instance.DestinationConnections =
						serializer.DeserializeDict(reader, (s, r) => r.GetValueAsInt(),
						                                                            (s, r) => s.DeserializeArray<ConnectorInfo>(r))
						          .ToDictionary();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
