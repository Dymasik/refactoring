using System.Runtime.Serialization;

namespace DataManagmentSystem.Common.Locale
{
	[DataContract]
	public class LocaleModel
	{
		[DataMember]
		public string Code { get; set; }
		[DataMember]
		public string NativeName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string TwoLetterISOLanguageName { get; set; }
	}
}
