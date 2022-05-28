using System.Runtime.Serialization;
using System.Globalization;

namespace DataManagmentSystem.Common.Locale
{
    [DataContract]
    public class DetailedLocaleModel : LocaleModel
    {
		[DataMember]
		public NumberFormatInfo NumberFormat { get; set; }

		[DataMember]
		public DateTimeFormatInfo DateTimeFormat { get; set; }

		[DataMember]
		public Calendar Calendar { get; set; }

		[DataMember]
		public TextInfo TextInfo { get; set; }
    }
}
