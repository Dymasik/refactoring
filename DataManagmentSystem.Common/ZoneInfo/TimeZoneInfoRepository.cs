using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using DataManagmentSystem.Common.ZoneInfo;
using System.Collections.ObjectModel;
using System.Linq;

namespace DataManagmentSystem.Common.ZoneInfo
{
	public class TimeZoneInfoRepository : IZoneInfoRepository
	{

		private static ReadOnlyCollection<TimeZoneInfo> _systemTimeZones => TimeZoneInfo.GetSystemTimeZones();

		public List<SelectListItem> GetSelectListItems() {
			return _systemTimeZones
				.Select(tz => new SelectListItem { Value = tz.Id, Text = tz.DisplayName })
				.ToList();
		}

		public List<ZoneInfoModel> GetZoneInfos() {
			return _systemTimeZones
				.Select(tz => new ZoneInfoModel {
					Id = tz.Id,
					BaseUtcOffset = tz.BaseUtcOffset,
					DaylightName = tz.DaylightName,
					DisplayName = tz.DisplayName,
					StandardName = tz.StandardName,
					SupportsDaylightSavingTime = tz.SupportsDaylightSavingTime
				}).ToList();
		}

		public ZoneInfoModel GetZoneInfo(string id) {
			var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
			if (tz != null) {
				return new ZoneInfoModel {
					Id = tz.Id,
					BaseUtcOffset = tz.BaseUtcOffset,
					DaylightName = tz.DaylightName,
					DisplayName = tz.DisplayName,
					StandardName = tz.StandardName,
					SupportsDaylightSavingTime = tz.SupportsDaylightSavingTime
				};
			}
			return null;
		}
	}
}
