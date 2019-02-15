namespace ICD.Connect.Krang.SPlus.Themes.UIs.SPlusMultiRoomRouting
{
	public static class Joins
	{
		public const ushort DIGITAL_CONTROL_SOURCE = 4;
		public const ushort DIGITAL_OFF = 5;

		#region Room Group

		public const ushort SMARTOBJECT_ROOM_GROUP = 102;

		#endregion

		#region Rooms

		public const ushort SMARTOBJECT_ROOMS = 100;

		public const ushort DIGITAL_ROOMS_OFFSET = 6;
		public const ushort DIGITAL_ROOMS_SELECT = 1;
		public const ushort DIGITAL_ROOMS_VOLUME_UP = 2;
		public const ushort DIGITAL_ROOMS_VOLUME_DOWN = 3;
		public const ushort DIGITAL_ROOMS_MUTE = 4;

		public const ushort ANALOG_ROOMS_OFFSET = 1;
		public const ushort ANALOG_ROOMS_VOLUME = 1;

		public const ushort SERIAL_ROOMS_OFFSET = 2;
		public const ushort SERIAL_ROOMS_NAME = 1;
		public const ushort SERIAL_ROOMS_SOURCE = 2;

		#endregion

		#region Sources

		public const ushort SMARTOBJECT_SOURCES = 101;

		public const ushort DIGITAL_SOURCES_OFFSET = 1;
		public const ushort DIGITAL_SOURCES_SELECT = 1;

		public const ushort SERIAL_SOURCES_OFFSET = 2;
		public const ushort SERIAL_SOURCES_NAME = 1;
		public const ushort SERIAL_SOURCES_ROOMS = 2;

		#endregion

		#region Dynamic Button List

		public const ushort DYNAMIC_BUTTON_LIST_NUMBER_OF_ITEMS_JOIN = 4;
		public const ushort DYNAMIC_BUTTON_LIST_DIGITAL_START_SELECTED_JOIN = 11;
		public const ushort DYNAMIC_BUTTON_LIST_SERIAL_START_TEXT_JOIN = 11;

		#endregion

		#region SRL

		public const ushort SRL_NUMBER_OF_ITEMS_JOIN = 3;

		private const ushort START_DIGITAL_JOIN = 4011;
		private const ushort START_ANALOG_JOIN = 11;
		private const ushort START_SERIAL_JOIN = 11;

		/// <summary>
		/// Gets the digital join offset for the given control.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="increment"></param>
		/// <param name="join"></param>
		/// <returns></returns>
		public static ushort GetDigitalJoinOffset(int index, ushort increment, ushort join)
		{
			return (ushort)((START_DIGITAL_JOIN - 1) + (index * increment) + join);
		}

		/// <summary>
		/// Gets the analog join offset for the given control.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="increment"></param>
		/// <param name="join"></param>
		/// <returns></returns>
		public static ushort GetAnalogJoinOffset(int index, ushort increment, ushort join)
		{
			return (ushort)((START_ANALOG_JOIN - 1) + (index * increment) + join);
		}

		/// <summary>
		/// Gets the serial join offset for the given control.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="increment"></param>
		/// <param name="join"></param>
		/// <returns></returns>
		public static ushort GetSerialJoinOffset(int index, ushort increment, ushort join)
		{
			return (ushort)((START_SERIAL_JOIN - 1) + (index * increment) + join);
		}

		public static ushort GetDigitalJoinFromOffset(ushort joinWithOffset, ushort increment, out int index)
		{
			index = (joinWithOffset - START_DIGITAL_JOIN) / increment;
			return (ushort)((joinWithOffset - START_DIGITAL_JOIN) % increment + 1);
		}

		public static ushort GetAnalogJoinFromOffset(ushort joinWithOffset, ushort increment, out int index)
		{
			index = (joinWithOffset - START_ANALOG_JOIN) / increment;
			return (ushort)((joinWithOffset - START_ANALOG_JOIN) % increment + 1);
		}

		public static ushort GetSerialJoinFromOffset(ushort joinWithOffset, ushort increment, out int index)
		{
			index = (joinWithOffset - START_SERIAL_JOIN) / increment;
			return (ushort)((joinWithOffset - START_SERIAL_JOIN) % increment + 1);
		}

		#endregion
	}
}
