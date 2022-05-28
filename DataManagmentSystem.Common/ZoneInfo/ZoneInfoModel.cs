using System.Runtime.Serialization;
using System;
using TimeZoneConverter;
using Microsoft.Extensions.Logging;

namespace DataManagmentSystem.Common.ZoneInfo
{
	[DataContract]
	public class ZoneInfoModel
	{

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public TimeSpan BaseUtcOffset { get; set; }

		[DataMember]
		public string DaylightName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string StandardName { get; set; }

		[DataMember]
		public bool SupportsDaylightSavingTime { get; set; }

		public ZoneInfoModel() { }

		public ZoneInfoModel(string id, ILogger logger){
			Id = id;
			DisplayName = id;
			try {
				var timeZoneInfo = TZConvert.GetTimeZoneInfo(id);
				BaseUtcOffset = timeZoneInfo.BaseUtcOffset;
				DaylightName = timeZoneInfo.DaylightName;
				StandardName = timeZoneInfo.StandardName;
				SupportsDaylightSavingTime = timeZoneInfo.SupportsDaylightSavingTime;
			} catch(TimeZoneNotFoundException e){
				logger.LogError(e, $"Time zone {id} was not found using TZConvert");
			}
		}
	}
}
