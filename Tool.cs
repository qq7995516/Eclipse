using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Eclipse
{
    public static class Tool
    {
        /// <summary>
        /// 获取枚举成员的 DescriptionAttribute 描述文本。
        /// </summary>
        /// <param name="value">枚举值。</param>
        /// <returns>如果定义了 DescriptionAttribute，则返回其描述；否则返回枚举成员的名称。</returns>
        public static string GetDescription(this Enum value)
        {
            // 获取枚举的类型
            Type type = value.GetType();

            // 获取枚举成员的名称
            string? memberName = Enum.GetName(type, value);
            if (memberName == null)
            {
                return value.ToString();
            }

            // 获取该成员的 FieldInfo 对象
            FieldInfo? fieldInfo = type.GetField(memberName);
            if (fieldInfo == null)
            {
                return memberName;
            }

            // 查找 DescriptionAttribute
            var attribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                     .FirstOrDefault() as DescriptionAttribute;

            // 如果找到了特性，返回其 Description 属性；否则返回成员名称
            return attribute?.Description ?? memberName;
        }
    }
}
