#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Json;

namespace ICD.Connect.Krang.Remote.Broadcast.OriginatorsChange
{
	[JsonConverter(typeof(OriginatorsChangeDataConverter))]
	public sealed class OriginatorsChangeData
	{
	}

	public sealed class OriginatorsChangeDataConverter : AbstractGenericJsonConverter<OriginatorsChangeData>
	{
	}
}
