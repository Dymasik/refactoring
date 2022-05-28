namespace DataManagmentSystem.Common.Macros
{
	using System.Text.RegularExpressions;
	using DataManagmentSystem.Auth.Injector.User;
	using DataManagmentSystem.Auth.Injector;

	public class CurrentUserAttributeMacrosValueProvider : IMacrosValueProvider
	{
		private readonly IUserDataAccessor _userDataAccessor;
		private UserModel _userModel;
		private static readonly Regex BASE_MACROS_TEMPLATE = new Regex(@"\[#CUSTOM_USER_INFO:(?<attribute>\w+)#\]");
		private const string CUSTOM_ATTRIBUTE_TPL = "custom:{0}";
		private string _attributeName;

		private UserModel UserModel {
			get {
				if (_userModel == null) {
					_userModel = _userDataAccessor.GetCurrentUserInfo().Result;
				}
				return _userModel;
			}
		}

		public CurrentUserAttributeMacrosValueProvider(IUserDataAccessor userDataAccessor) {
			_userDataAccessor = userDataAccessor;
		}

		public bool IsApplicableTo(string macrosName) {
			if (string.IsNullOrWhiteSpace(macrosName)) {
				return false;
			}
			InitializeCustomAttributeName(macrosName);
			return !string.IsNullOrWhiteSpace(_attributeName) && CheckAttributeExistsInUserModel();
		}

		public object GetValue() {
			return UserModel.CustomAttributes[string.Format(CUSTOM_ATTRIBUTE_TPL, _attributeName)];
		}

		private bool CheckAttributeExistsInUserModel() {
			return UserModel.CustomAttributes.ContainsKey(
				string.Format(CUSTOM_ATTRIBUTE_TPL, _attributeName)
			);
		}

		private void InitializeCustomAttributeName(string macrosName) {
			var match = BASE_MACROS_TEMPLATE.Match(macrosName);
			if (!match.Success)
				return;
			var attributeName = match.Groups["attribute"]?.Value;
			if (!string.IsNullOrWhiteSpace(attributeName))
				_attributeName = attributeName;
		}
	}
}