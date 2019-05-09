using ICD.Common.Utils.Json;
using Newtonsoft.Json;

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
