using System.ComponentModel;
using System.Reflection;

namespace SkullKingCore.Utility
{
    public static class Misc
    {

        public static string GetEnumLabel<TEnum>(TEnum value) where TEnum : Enum
        {
            var name = value.ToString();

            var mem = typeof(TEnum).GetMember(name);

            if (mem.Length > 0)
            {
                var desc = mem[0].GetCustomAttribute<DescriptionAttribute>();

                if (desc != null && !string.IsNullOrWhiteSpace(desc.Description))
                    return desc.Description;
            }

            return name;
        }

    }
}
