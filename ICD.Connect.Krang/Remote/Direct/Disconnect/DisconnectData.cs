using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Krang.Remote.Direct.Disconnect
{
	[JsonConverter(typeof(DisconnectDataConverter))]
	public sealed class DisconnectData
	{
	}

	public sealed class DisconnectDataConverter : AbstractGenericJsonConverter<DisconnectData>
	{
	}
}
