using System.Globalization;

namespace BlazorNumericTextBox;

public class NumericTextBoxDefaults
{
    public static bool SelectOnEntry { get; set; }
    public static int MaxLength { get; set; } = 12;
    public static CultureInfo Culture { get; set; } = new CultureInfo("en-US");
    public static string CustomDecimalSeparator { get; set; } = string.Empty;
}
