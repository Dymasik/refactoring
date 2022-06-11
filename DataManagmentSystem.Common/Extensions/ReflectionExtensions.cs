namespace DataManagmentSystem.Common.Extensions
{
	using DataManagmentSystem.Common.Attributes;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.ComponentModel.DataAnnotations;
	using DataManagmentSystem.Common.CoreEntities;
	using System;
	using System.Reflection;
	using System.Linq;
	using System.Collections.Generic;
	using Microsoft.EntityFrameworkCore.Metadata;
	using Microsoft.Extensions.Localization;

	public static class ReflectionExtensions
	{
		private static List<string> _baseEntityColumnNamesCache = null;
		private static List<string> _baseEntityColumnNames 
			=> _baseEntityColumnNamesCache ?? (_baseEntityColumnNamesCache = typeof(BaseEntity).GetProperties().Select(property => property.Name).ToList());
		public static bool IsBaseColumn(this IProperty property) {
			return _baseEntityColumnNames.Contains(property.Name);
		}

		public static object GetDefaultValue(this PropertyInfo propertyInfo) {
			Type type = propertyInfo.GetType();
			if (type.IsValueType) {
				return Activator.CreateInstance(type);
			}
			return null;
		}

		public static bool IsLocalizedEntity(this Type type) {
			return type.GetCustomAttribute(typeof(TranslationStoreAttribute)) != null;
		}
		public static bool IsLocalizedField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(LocalizedAttribute)) != null;
		}
		public static bool IsLookupField(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.IsSubclassOf(typeof(BaseEntity))
			&& !propertyInfo.IsImageField()
			&& !propertyInfo.IsMultiCurrencyAmountField();
		}
		public static bool IsGuidField(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.Equals(typeof(Guid?)) || propertyInfo.PropertyType.Equals(typeof(Guid));
		}
		public static bool IsBooleanField(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.Equals(typeof(bool?)) || propertyInfo.PropertyType.Equals(typeof(bool));
		}
		public static bool IsTextField(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.Equals(typeof(string));
		}
		public static bool IsDateTimeField(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.Equals(typeof(DateTime));
		}
		public static bool IsImageField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(ImageAttribute)) != null;
		}
		public static bool IsMultiCurrencyAmountField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(MultiCurrencyAmountAttribute)) != null;
		}
		public static string GetEntityDisplayValueField(this Type type) {
			return type.GetProperties().FirstOrDefault(p => p.IsDisplayValueField())?.Name;
		}
		public static bool IsDateOnlyField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(DateOnlyAttribute)) != null;
		}
		public static bool IsTimeOnlyField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(TimeOnlyAttribute)) != null;
		}
		public static bool IsReadOnlyField(this PropertyInfo propertyInfo) {
			return propertyInfo.IsCalculatedField()
				|| propertyInfo.GetCustomAttribute(typeof(KeyAttribute)) != null
				|| propertyInfo.GetCustomAttribute(typeof(DatabaseGeneratedAttribute)) != null
				|| propertyInfo.GetCustomAttribute(typeof(ReadOnlyAttribute)) != null;
		}

        public static bool IsCalculatedField(this PropertyInfo propertyInfo) {
			return !propertyInfo.HasSetter() || propertyInfo.SetMethod.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() == null;
		}

        public static bool HasSetter(this PropertyInfo propertyInfo) {
			return propertyInfo.SetMethod != null;
		}
		
		public static bool IsRequiredField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(RequiredAttribute)) != null;
		}
		public static string GetRequiredFieldErrorMessage(this PropertyInfo propertyInfo, IStringLocalizer stringLocalizer) {
			var requiredAttribute = propertyInfo.GetCustomAttribute<RequiredAttribute>();
			if(requiredAttribute != null){
				if(requiredAttribute.ErrorMessage != null){
					return stringLocalizer.GetString(requiredAttribute.ErrorMessage).Value;
				}
				return requiredAttribute.FormatErrorMessage(propertyInfo.GetCaption(stringLocalizer));
			}
			return null;
		}
		public static string GetCaption(this Type type, IStringLocalizer stringLocalizer) {
			var displayAttribute = type.GetCustomAttribute<DisplayAttribute>();
			return displayAttribute != null ? stringLocalizer.GetString(displayAttribute.Name).Value : type.GetEntityTableName();
		}
		public static string GetCaption(this PropertyInfo propertyInfo, IStringLocalizer stringLocalizer) {
			var displayAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
			return displayAttribute != null ? stringLocalizer.GetString(displayAttribute.Name).Value : propertyInfo.Name;
		}
		public static bool IsDisplayValueField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(DisplayValueAttribute)) != null;
		} 
		public static bool IsEntityCollectionProperty(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.IsGenericType 
				&& propertyInfo.PropertyType.GetGenericTypeDefinition().Equals(typeof(ICollection<>))
				&& propertyInfo.PropertyType.GenericTypeArguments[0].IsSubclassOf(typeof(BaseEntity));
		}
		public static string GetEntityCollectionTableName(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.GenericTypeArguments[0].GetEntityTableName();
		}
		public static bool IsHTMLField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(HTMLAttribute)) != null;
		}
		public static bool IsFileSourceField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(FileSourceAttribute)) != null;
		}
		public static bool IsImageAltField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(ImageAltAttribute)) != null;
		}
		public static string GetImageSourceFieldName(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.GetProperties().FirstOrDefault(p => p.IsFileSourceField())?.Name;
		}
		public static string GetImageAltFieldName(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.GetProperties().FirstOrDefault(p => p.IsImageAltField())?.Name;
		}
		public static bool IsBaseCurrencyAmountField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(BaseCurrencyAmountAttribute)) != null;
		}
		public static string GetBaseCurrencyAmountFieldName(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.GetProperties().FirstOrDefault(p => p.IsBaseCurrencyAmountField())?.Name;
		}
		public static bool IsCurrencyField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(CurrencyAttribute)) != null;
		}
		public static string GetCurrencyFieldName(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.GetProperties().FirstOrDefault(p => p.IsCurrencyField())?.Name;
		}
		public static bool IsAmountField(this PropertyInfo propertyInfo) {
			return propertyInfo.GetCustomAttribute(typeof(AmountAttribute)) != null;
		}
		public static string GetAmountFieldName(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.GetProperties().FirstOrDefault(p => p.IsAmountField())?.Name;
		}
		public static string GetDbFieldName(this PropertyInfo propertyInfo) {
			if(propertyInfo.PropertyType.IsSubclassOf(typeof(BaseEntity))){
				return propertyInfo.GetForeignKeyAttributeValue() 
					?? propertyInfo.ReflectedType.GetProperties().FirstOrDefault(p => p.GetForeignKeyAttributeValue() == propertyInfo.Name)?.Name
					?? $"{propertyInfo.Name}Id";
			}
			return propertyInfo.Name;
		}
		public static string GetForeignKeyAttributeValue(this PropertyInfo propertyInfo) {
			return (propertyInfo.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute)?.Name;
		}
		public static int GetTextFieldMaxLength(this PropertyInfo propertyInfo) {
			return (propertyInfo.GetCustomAttribute(typeof(StringLengthAttribute)) as StringLengthAttribute)?.MaximumLength ?? -1;
		}
		public static string GetTextFieldMask(this PropertyInfo propertyInfo) {
			return (propertyInfo.GetCustomAttribute(typeof(MaskAttribute)) as MaskAttribute)?.Mask;
		}
		public static int GetTextFieldMinLength(this PropertyInfo propertyInfo) {
			return (propertyInfo.GetCustomAttribute(typeof(StringLengthAttribute)) as StringLengthAttribute)?.MinimumLength ?? -1;
		}
		public static bool IsNumberField(this PropertyInfo propertyInfo) {
			return propertyInfo.PropertyType.Equals(typeof(int)) 
                    || propertyInfo.PropertyType.Equals(typeof(long)) 
                    || propertyInfo.PropertyType.Equals(typeof(short)) 
                    || propertyInfo.PropertyType.Equals(typeof(sbyte)) 
                    || propertyInfo.PropertyType.Equals(typeof(ulong)) 
                    || propertyInfo.PropertyType.Equals(typeof(uint)) 
                    || propertyInfo.PropertyType.Equals(typeof(ushort))
                    || propertyInfo.PropertyType.Equals(typeof(byte))
                    || propertyInfo.PropertyType.Equals(typeof(double)) 
                    || propertyInfo.PropertyType.Equals(typeof(float))
                    || propertyInfo.PropertyType.Equals(typeof(decimal));
		}
		public static object GetNumberFieldMinValue(this PropertyInfo propertyInfo) {
			var rangeAttribute = propertyInfo.GetCustomAttribute(typeof(RangeAttribute)) as RangeAttribute;
			if(rangeAttribute != null){
				return rangeAttribute.Minimum;
			}
			if(propertyInfo.PropertyType.Equals(typeof(sbyte))) {
				return sbyte.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(short))) {
				return short.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(int))) {
				return int.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(long))) {
				return long.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(byte))) {
				return byte.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(ushort))) {
				return ushort.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(uint))) {
				return uint.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(ulong))) {
				return ulong.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(double))) {
				return double.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(float))) {
				return float.MinValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(decimal))) {
				return decimal.MinValue;
			}
			return null;
		}
		public static object GetNumberFieldMaxValue(this PropertyInfo propertyInfo) {
			var rangeAttribute = propertyInfo.GetCustomAttribute(typeof(RangeAttribute)) as RangeAttribute;
			if(rangeAttribute != null){
				return rangeAttribute.Maximum;
			}
			if(propertyInfo.PropertyType.Equals(typeof(sbyte))) {
				return sbyte.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(short))) {
				return short.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(int))) {
				return int.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(long))) {
				return long.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(byte))) {
				return byte.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(ushort))) {
				return ushort.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(uint))) {
				return uint.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(ulong))) {
				return ulong.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(double))) {
				return double.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(float))) {
				return float.MaxValue;
			}
			else if(propertyInfo.PropertyType.Equals(typeof(decimal))) {
				return decimal.MaxValue;
			}
			return null;
		}

		public static int GetNumberFieldPrecision(this PropertyInfo propertyInfo) {
			var value = (propertyInfo.GetCustomAttribute(typeof(PrecisionAttribute)) as PrecisionAttribute)?.Value;
			if (value.HasValue) {
				return value.Value;
			} else {
				if (propertyInfo.PropertyType.Equals(typeof(double)))
					return 14;
				else if (propertyInfo.PropertyType.Equals(typeof(float)))
					return 6;
				else
					return propertyInfo.PropertyType.Equals(typeof(decimal)) ? 28 : -1;
			}
		}
		public static int GetNumberFieldScale(this PropertyInfo propertyInfo) {
			return (propertyInfo.GetCustomAttribute(typeof(ScaleAttribute)) as ScaleAttribute)?.Value ?? -1;
		}
		public static string GetReferencedEntitiesInverseProperty(this PropertyInfo propertyInfo) {
			return (propertyInfo.GetCustomAttribute(typeof(InversePropertyAttribute)) as InversePropertyAttribute)?.Property
				?? propertyInfo.PropertyType.GenericTypeArguments[0].GetProperties().FirstOrDefault(p => p.PropertyType.Equals(propertyInfo.DeclaringType)).Name;
		}
		public static string GetEntityTableName(this Type type) {
			return (type.GetCustomAttribute(typeof(TableAttribute)) as TableAttribute).Name;
		}
	}
}
