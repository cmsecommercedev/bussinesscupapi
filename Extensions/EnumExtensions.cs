using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection; // Reflection kullanmak için eklendi

namespace BussinessCupApi.Extensions // Projenizin ana namespace'ine uygun şekilde değiştirin veya bu şekilde bırakın
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            // Enum değerinin string karşılığını al
            var enumMember = enumValue.ToString();

            // Enum tipinin FieldInfo nesnesini al
            var fieldInfo = enumValue.GetType().GetField(enumMember);

            // FieldInfo null ise (geçersiz enum değeri?), string karşılığını dön
            if (fieldInfo == null)
            {
                return enumMember;
            }

            // Field üzerindeki DisplayAttribute'u ara
            var displayAttributes = fieldInfo.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];

            // Attribute bulunamazsa veya Name özelliği boşsa, string karşılığını dön
            if (displayAttributes == null || displayAttributes.Length == 0 || string.IsNullOrEmpty(displayAttributes[0].Name))
            {
                return enumMember;
            }
            // DisplayAttribute'un Name özelliğini dön
            else
            {
                return displayAttributes[0].Name;
            }
        }
    }
} 