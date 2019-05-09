using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Settings.Cores;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Broadcast.CoreDiscovery
{
	[JsonConverter(typeof(CoreDiscoveryDataConverter))]
	public sealed class CoreDiscoveryData
	{
		public int Id { get; set; }
		public string Name { get; set; }

		public static CoreDiscoveryData ForCore(ICore core)
		{
			return new CoreDiscoveryData
			{
				Id = core.Id,
				Name = core.Name
			};
		}
	}

	public sealed class CoreDiscoveryDataConverter : AbstractGenericJsonConverter<CoreDiscoveryData>
	{
		private const string ATTR_ID = "Id";
		private const string ATTR_NAME = "Name";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CoreDiscoveryData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Id != 0)
				writer.WriteProperty(ATTR_ID, value.Id);

			if (value.Name != null)
				writer.WriteProperty(ATTR_NAME, value.Name);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, CoreDiscoveryData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ID:
					instance.Id = reader.GetValueAsInt();
					break;

				case ATTR_NAME:
					instance.Name = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
