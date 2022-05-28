namespace DataManagmentSystem.Common.Macros {
    using DataManagmentSystem.Auth.Injector;

    public class CurrentUserNameMacrosValueProvider : IMacrosValueProvider {
        private readonly IUserDataAccessor _userDataAccessor;
        private const string BASE_MACROS_NAME = "[#CURRENT_USER_NAME#]";

        public CurrentUserNameMacrosValueProvider(IUserDataAccessor userDataAccessor)
        {
            _userDataAccessor = userDataAccessor;
        }

        public bool IsApplicableTo(string macrosName)
        {
            return BASE_MACROS_NAME.Equals(macrosName);
        }

        public object GetValue() {
            var currentUserInfo = _userDataAccessor.GetCurrentUserInfo().Result;
            return currentUserInfo.UserName;
        }
    } 
}