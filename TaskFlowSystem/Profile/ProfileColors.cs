using System.Linq;
using System.Windows.Media;

namespace TaskFlowSystem
{
    /// <summary>
    /// Акцентные цвета профилей: ключ сохраняется в JSON, а кисть строится при отображении.
    /// </summary>
    public static class ProfileColors
    {
        public const string Default = "Green";

        public static readonly string[] All = { "Green", "Blue", "Violet", "Amber", "Rose" };

        public static LinearGradientBrush CreateBrush(string colorKey)
        {
            Color from;
            Color to;

            switch ((colorKey ?? Default).Trim().ToLowerInvariant())
            {
                case "blue":
                    from = Color.FromRgb(0x5C, 0xC0, 0xFF);
                    to = Color.FromRgb(0x4A, 0x86, 0xFF);
                    break;
                case "violet":
                    from = Color.FromRgb(0xB8, 0xA0, 0xFF);
                    to = Color.FromRgb(0x9B, 0x82, 0xFF);
                    break;
                case "amber":
                    from = Color.FromRgb(0xFF, 0xDA, 0x7E);
                    to = Color.FromRgb(0xFF, 0xB3, 0x45);
                    break;
                case "rose":
                    from = Color.FromRgb(0xFF, 0xAE, 0xBA);
                    to = Color.FromRgb(0xFF, 0x86, 0x9C);
                    break;
                default:
                    from = Color.FromRgb(0x3D, 0xDC, 0x97);
                    to = Color.FromRgb(0x0F, 0xA5, 0x83);
                    break;
            }

            var brush = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 1) };
            brush.GradientStops.Add(new GradientStop(from, 0));
            brush.GradientStops.Add(new GradientStop(to, 1));
            brush.Freeze();
            return brush;
        }

        public static string Normalize(string colorKey)
        {
            if (string.IsNullOrWhiteSpace(colorKey))
            {
                return Default;
            }

            var value = colorKey.Trim();
            return All.Contains(value, System.StringComparer.OrdinalIgnoreCase)
                ? value
                : Default;
        }
    }
}
