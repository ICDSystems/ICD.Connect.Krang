#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;

namespace ICD.Connect.Krang.Remote.Direct.RouteDevices
{
	[JsonConverter(typeof(RouteDevicesResultConverter))]
	public sealed class RouteDevicesData
	{
		public bool Result { get; set; }
	}

	public sealed class RouteDevicesResultConverter : AbstractGenericJsonConverter<RouteDevicesData>
	{
		private const string ATTR_RESULT = "r";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, RouteDevicesData value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Result)
				writer.WriteProperty(ATTR_RESULT, value.Result);
		}

		/// <summary>
		/// Override to handle the current property value with the given name.
		/// </summary>
		/// <param name="property"></param>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		/// <param name="serializer"></param>
		protected override void ReadProperty(string property, JsonReader reader, RouteDevicesData instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_RESULT:
					instance.Result = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
