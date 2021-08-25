#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Krang.Remote.Direct.API;
using NUnit.Framework;

namespace ICD.Connect.Krang.Tests.Remote.Direct.API
{
	[TestFixture]
	public sealed class ApiMessageDataTest
	{
		[Test]
		public void DeserializeTest()
		{
			const string data = @"{
	""c"": {
		""ns"": [
		{
			""n"": ""ControlSystem"",
			""no"": {
				""ns"": [
				{
					""n"": ""Core"",
					""no"": {
						""ngs"": [
						{
							""n"": ""Devices"",
							""ns"": {
								""200001"": {
									""ngs"": [
									{
										""n"": ""Controls"",
										""ns"": {
											""0"": {
												""es"": [
												{
													""n"": ""OnActiveInputsChanged"",
													""r"": {},
													""a"": 1
												},
												{
													""n"": ""OnSourceDetectionStateChange"",
													""r"": {},
													""a"": 1
												}
												],
												""ms"": [
												{
													""n"": ""GetCurrentRouteState"",
													""r"": {
														""t"": ""ICD.Connect.Routing.SPlus.SPlusDestinationDevice.RouteState.RouteState, ICD.Connect.Routing.SPlus"",
														""v"": {
															""inputsDetected"": [
															1
																]
														}
													},
													""e"": true
												}
												],
												""ps"": [
												{
													""n"": ""Name"",
													""r"": {
														""t"": ""System.String"",
														""v"": ""SPlusDestinationRouteControl""
													},
													""rw"": 1
												}
												]
											}
										}
									}
									]
								}
							}
						}
						]
					}
				}
				]
			}
		}
		]
	},
	""r"": true
}";

			ApiMessageData messageData = JsonConvert.DeserializeObject<ApiMessageData>(data);

			Assert.NotNull(messageData.Command);
			Assert.IsTrue(messageData.IsResponse);
		}
	}
}
