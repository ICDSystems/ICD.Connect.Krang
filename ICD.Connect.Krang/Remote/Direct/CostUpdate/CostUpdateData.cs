using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Direct.CostUpdate
{
	[JsonConverter(typeof(CostUpdateDataConverter))]
	public sealed class CostUpdateData
	{
		public Dictionary<int, double> SourceCosts { get; set; }
		public Dictionary<int, double> DestinationCosts { get; set; }
	}

	public sealed class CostUpdateDataConverter : AbstractGenericJsonConverter<CostUpdateData>
	{
		private const string ATTR_SOURCE_COSTS = "s";
		private const string ATTR_DESTINATION_COSTS = "d";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CostUpdateData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.SourceCosts != null)
			{
				writer.WritePropertyName(ATTR_SOURCE_COSTS);
				serializer.SerializeDict(writer, value.SourceCosts);
			}

			if (value.DestinationCosts != null)
			{
				writer.WritePropertyName(ATTR_DESTINATION_COSTS);
				serializer.SerializeDict(writer, value.DestinationCosts);
			}
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, CostUpdateData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SOURCE_COSTS:
					instance.SourceCosts = serializer.DeserializeDict<int, double>(reader).ToDictionary();
					break;

				case ATTR_DESTINATION_COSTS:
					instance.DestinationCosts = serializer.DeserializeDict<int, double>(reader).ToDictionary();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
