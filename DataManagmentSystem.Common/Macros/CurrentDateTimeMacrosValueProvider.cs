namespace DataManagmentSystem.Common.Macros {
    using System;

    public class CurrentDateTimeMacrosValueProvider : IMacrosValueProvider {
        private const string BASE_MACROS_NAME = "[#NOW#]";

        public bool IsApplicableTo(string macrosName)
        {
            return BASE_MACROS_NAME.Equals(macrosName);
        }

        public object GetValue() {
            return DateTime.UtcNow;
        }
    } 
}