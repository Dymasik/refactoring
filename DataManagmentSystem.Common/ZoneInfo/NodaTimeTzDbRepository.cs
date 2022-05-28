using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using DataManagmentSystem.Common.ZoneInfo;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using NodaTime;

namespace DataManagmentSystem.Common.ZoneInfo
{
	public class NodaTimeTzDbRepository : IZoneInfoRepository
	{

		private readonly ILogger<NodaTimeTzDbRepository> _logger;

		private ReadOnlyCollection<string> _tzDbIds => DateTimeZoneProviders.Tzdb.Ids;

		public NodaTimeTzDbRepository(ILogger<NodaTimeTzDbRepository> logger) {
			_logger = logger;
		}

		public List<SelectListItem> GetSelectListItems() {
			return _tzDbIds
				.Select(id => new ZoneInfoModel(id, _logger))
				.Select(tz => new SelectListItem { Value = tz.Id, Text = tz.DisplayName })
				.ToList();
		}

		public List<ZoneInfoModel> GetZoneInfos() {
			return _tzDbIds
				.Select(id => new ZoneInfoModel(id, _logger)).ToList();
		}

		public ZoneInfoModel GetZoneInfo(string id) {
			return DateTimeZoneProviders.Tzdb.GetZoneOrNull(id) != null
				? new ZoneInfoModel(id, _logger)
				: null;
		}
	}
}
