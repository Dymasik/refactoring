using System;
using System.Collections.Generic;
using DataManagmentSystem.Common.Locale;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DataManagmentSystem.Common.Locale
{
    public interface ILocaleRepository
	{
		List<SelectListItem> GetSelectListItems();
		List<LocaleModel> GetLocales();
		List<DetailedLocaleModel> GetDetailedLocales();
		DetailedLocaleModel GetDetailedLocale(string code);
		LocaleModel GetLocale(string code);
	}
}
