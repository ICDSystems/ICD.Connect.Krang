using ICD.Connect.Protocol.Network.Direct;

namespace ICD.Connect.Krang.Remote.Direct
{
	public class GenericMessage<T> : AbstractMessage
	{
		public T Value { get; set; }
	}
}
