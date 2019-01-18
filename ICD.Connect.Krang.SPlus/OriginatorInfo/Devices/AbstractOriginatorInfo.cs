using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Krang.SPlus.OriginatorInfo.Devices
{
	public abstract class AbstractOriginatorInfo
	{

		public int Id { get; set; }

		public string Name { get; set; }

		protected AbstractOriginatorInfo()
		{
			
		}

		protected AbstractOriginatorInfo(int id, string name)
		{
			Id = id;
			Name = name;
		}

		protected AbstractOriginatorInfo(IOriginator originator)
		{
			if (originator == null)
				return;

			Id = originator.Id;
			Name = originator.Name;
		}
	}
}