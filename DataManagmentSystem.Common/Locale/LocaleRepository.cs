using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using DataManagmentSystem.Common.Locale;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DataManagmentSystem.Common.Locale
{
	public class CultureInfoLocaleRepository : ILocaleRepository
	{

		private CultureInfo[] _cultures => CultureInfo.GetCultures(CultureTypes.AllCultures);

		public List<SelectListItem> GetSelectListItems() => _cultures
			.Select(culture =>
				new SelectListItem { Value = culture.Name, Text = culture.NativeName }
			).ToList();

		public List<LocaleModel> GetLocales() => _cultures
			.Select(culture => new LocaleModel {
				Code = culture.Name,
				NativeName = culture.NativeName,
				DisplayName = culture.DisplayName,
				TwoLetterISOLanguageName = culture.TwoLetterISOLanguageName
			}).ToList();


		public List<DetailedLocaleModel> GetDetailedLocales() => _cultures
			.Select(culture => new DetailedLocaleModel {
				Code = culture.Name,
				NativeName = culture.NativeName,
				DisplayName = culture.DisplayName,
				TwoLetterISOLanguageName = culture.TwoLetterISOLanguageName,
				NumberFormat = culture.NumberFormat,
				DateTimeFormat = culture.DateTimeFormat,
				Calendar = culture.Calendar,
				TextInfo = culture.TextInfo
			}).ToList();

		public DetailedLocaleModel GetDetailedLocale(string code) {
			var culture = _cultures
			  .SingleOrDefault(culture => culture.Name == code);
			return culture == null ? null : new DetailedLocaleModel {
				Code = culture.Name,
				NativeName = culture.NativeName,
				DisplayName = culture.DisplayName,
				TwoLetterISOLanguageName = culture.TwoLetterISOLanguageName,
				NumberFormat = culture.NumberFormat,
				DateTimeFormat = culture.DateTimeFormat,
				Calendar = culture.Calendar,
				TextInfo = culture.TextInfo
			};
		}

		public LocaleModel GetLocale(string code) {
			var culture = _cultures
			  .SingleOrDefault(culture => culture.Name == code);
			return culture == null ? null : new LocaleModel {
				Code = culture.Name,
				NativeName = culture.NativeName,
				DisplayName = culture.DisplayName,
				TwoLetterISOLanguageName = culture.TwoLetterISOLanguageName
			};
		}
	}
}
