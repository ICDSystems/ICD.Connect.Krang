using System.Collections.Generic;
using ICD.Connect.Krang.SPlus.Rooms;

namespace ICD.Connect.Krang.SPlus.Themes
{
	interface IKrangAtHomeUserInterfaceFactory
	{
			/// <summary>
			/// Disposes the instantiated UIs.
			/// </summary>
			void Clear();

			/// <summary>
			/// Instantiates the user interfaces for the originators in the core.
			/// </summary>
			/// <returns></returns>
			void BuildUserInterfaces();

			/// <summary>
			/// Assigns the rooms to the existing user interfaces.
			/// </summary>
			void ReassignUserInterfaces();

			/// <summary>
			/// Assigns the rooms to the existing user interfaces.
			/// </summary>
			void AssignUserInterfaces(IEnumerable<IKrangAtHomeRoom> rooms);
	}
}