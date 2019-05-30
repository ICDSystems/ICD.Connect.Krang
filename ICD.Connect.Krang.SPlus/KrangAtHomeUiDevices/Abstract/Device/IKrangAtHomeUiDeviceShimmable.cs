using ICD.Connect.Devices.Simpl;

namespace ICD.Connect.Krang.SPlus.KrangAtHomeUiDevices.Abstract.Device
{
	public interface IKrangAtHomeUiDeviceShimmable : ISimplDevice
	{

		#region Methods

		void SetRoomId(int id);

		void SetAudioSourceId(int id);

		void SetVideoSourcdId(int id);

		void SetVolumeLevel(float volumeLevel);

		void SetVolumeRampUp();

		void SetVolumeRampDown();

		void SetVolumeRampStop();

		void SetVolumeMute(bool state);

		void SetVolumeMuteToggle();

		#endregion
	}
}