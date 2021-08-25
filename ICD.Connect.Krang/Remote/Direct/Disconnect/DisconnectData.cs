#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Json;

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
