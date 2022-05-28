using System;
using System.Collections.Generic;
using DataManagmentSystem.Common.ZoneInfo;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DataManagmentSystem.Common.ZoneInfo
{
	public interface IZoneInfoRepository
	{
		List<SelectListItem> GetSelectListItems();
		List<ZoneInfoModel> GetZoneInfos();
		ZoneInfoModel GetZoneInfo(string id);
	}
}
