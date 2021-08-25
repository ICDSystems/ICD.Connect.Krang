#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Krang.Remote.Broadcast.TielineDiscovery
{
	[JsonConverter(typeof(TielineDiscoveryDataConverter))]
	public sealed class TielineDiscoveryData
	{
		public Dictionary<int, int> DeviceIds { get; set; }
		public Dictionary<int, IEnumerable<Connection>> Tielines { get; set; }
	}

	public sealed class TielineDiscoveryDataConverter : AbstractGenericJsonConverter<TielineDiscoveryData>
	{
		private const string ATTR_DEVICE_IDS = "DeviceIds";
		private const string ATTR_TIELINES = "Tielines";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, TielineDiscoveryData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.DeviceIds != null)
				serializer.SerializeDict(writer, value.DeviceIds);

			if (value.Tielines != null)
				serializer.SerializeDict(writer, value.DeviceIds);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, TielineDiscoveryData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_DEVICE_IDS:
					instance.DeviceIds = serializer.DeserializeDict<int, int>(reader).ToDictionary();
					break;

				case ATTR_TIELINES:
					instance.Tielines = serializer.DeserializeDict<int, IEnumerable<Connection>>(reader).ToDictionary();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
