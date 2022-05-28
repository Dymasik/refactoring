namespace DataManagmentSystem.Common.Macros {
    using System;

    public interface IMacrosValueProvider {
        bool IsApplicableTo(string macrosName);
        object GetValue();
    } 
}