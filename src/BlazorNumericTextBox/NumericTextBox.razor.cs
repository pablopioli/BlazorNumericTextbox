using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorNumericTextBox
{
    public partial class NumericTextBox : ComponentBase
    {
        public static class Defaults
        {
            public static bool UseEnterAsTab { get; set; }
            public static bool SelectOnEntry { get; set; }
            public static int MaxLength { get; set; } = 12;
            public static CultureInfo Culture { get; set; } = new CultureInfo("en-US");
        }

        [Inject] IJSRuntime JsRuntime { get; set; }
        [Parameter] public string Id { get; set; }
        [Parameter] public string BaseClass { get; set; } = "form-control";
        [Parameter] public string Class { get; set; }
        [Parameter] public string Style { get; set; } = "";
        [Parameter] public int MaxLength { get; set; } = Defaults.MaxLength;
        [Parameter] public string Format { get; set; } = "";
        [Parameter] public decimal Value { get; set; } = 0;
        [Parameter] public bool UseEnterAsTab { get; set; } = Defaults.UseEnterAsTab;
        [Parameter] public bool SelectOnEntry { get; set; } = Defaults.SelectOnEntry;
        [Parameter] public CultureInfo Culture { get; set; }
        [Parameter] public Func<decimal, string> ConditionalFormatting { get; set; }
        [Parameter] public EventCallback<decimal> ValueChanged { get; set; }
        [Parameter] public EventCallback<decimal> NumberChanged { get; set; }

        private readonly string DecimalSeparator;

        private string VisibleValue = "";
        private string ActiveClass = "";

        private static Random Random = new Random();

        public NumericTextBox()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            Id = new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Next(s.Length)]).ToArray());

            ActiveClass = ComputeClass();

            if (Culture == null)
            {
                if (CultureInfo.DefaultThreadCurrentUICulture != null)
                {
                    Culture = CultureInfo.DefaultThreadCurrentUICulture;
                }
                else
                {
                    Culture = Defaults.Culture;
                }
            }

            DecimalSeparator = Culture.NumberFormat.NumberDecimalSeparator;
        }

        private async Task SetVisibleValue(decimal value)
        {
            if (string.IsNullOrEmpty(Format))
            {
                VisibleValue = value.ToString();
            }
            else
            {
                VisibleValue = value.ToString(Format);
            }

            var additionalFormatting = string.Empty;
            if (ConditionalFormatting != null)
            {
                additionalFormatting = ConditionalFormatting(value);
            }

            var prevClass = ActiveClass;
            ActiveClass = ComputeClass(additionalFormatting);

            if (prevClass != ActiveClass)
            {
                StateHasChanged();
            }

            await JsRuntime.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                string toDecimalSeparator = "";
                if (DecimalSeparator != ".")
                {
                    toDecimalSeparator = DecimalSeparator;
                }

                await JsRuntime.InvokeVoidAsync("ConfigureNumericTextBox",
                    new string[] {
                        "#" + Id,
                        ".",
                        toDecimalSeparator,
                        UseEnterAsTab ? "true" : "",
                        SelectOnEntry ? "true" : "",
                        MaxLength.ToString()
                    });

                await SetVisibleValue(Value);
                await JsRuntime.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });
            }
        }

        private decimal _previousValue;
        private async Task HasGotFocus()
        {
            _previousValue = Value;
            ActiveClass = ComputeClass();

            if (Value == 0)
            {
                await JsRuntime.InvokeVoidAsync("SelectNumericTextBoxContents", new string[] { "#" + Id, VisibleValue });
            }
        }

        private async Task HasLostFocus()
        {
            var data = await JsRuntime.InvokeAsync<string>("GetNumericTextBoxValue", new string[] { "#" + Id });

            var cleaned = string.Join("",
                data.Replace("(", "-").Where(x => char.IsDigit(x) ||
                                             x == '-' ||
                                             x.ToString() == DecimalSeparator).ToArray());
            var parsed = decimal.TryParse(cleaned, NumberStyles.Any, Culture.NumberFormat, out var valueAsDecimal);
            if (!parsed)
            {
                if (string.IsNullOrEmpty(Format))
                {
                    VisibleValue = "";
                }
                else
                {
                    VisibleValue = 0.ToString(Format);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(Format))
                {
                    VisibleValue = cleaned;
                }
                else
                {
                    VisibleValue = valueAsDecimal.ToString(Format);
                }
            }

            // Negative monetary values a represented with parenthesis
            cleaned = string.Join("",
                VisibleValue.Replace("(", "-")
                            .Where(x => char.IsDigit(x) ||
                                        x == '-' ||
                                        x.ToString() == DecimalSeparator).ToArray());

            parsed = decimal.TryParse(cleaned, NumberStyles.Any, Culture.NumberFormat, out var roundedValue);

            if (parsed)
            {
                Value = roundedValue;
            }
            else
            {
                Value = valueAsDecimal;
            }

            await SetVisibleValue(Value);

            await ValueChanged.InvokeAsync(Value);

            if (_previousValue != Value)
            {
                await NumberChanged.InvokeAsync(Value);
            }
        }

        private string ComputeClass(string additionalFormatting = "")
        {
            var cssClass = new StringBuilder();

            cssClass.Append(BaseClass);

            if (!string.IsNullOrEmpty(Class))
            {
                cssClass.Append(' ').Append(Class);
            }

            if (!string.IsNullOrEmpty(additionalFormatting))
            {
                cssClass.Append(' ').Append(additionalFormatting);
            }

            return cssClass.ToString();
        }

        public async Task SetValue(decimal value)
        {
            await SetVisibleValue(value);
            await ValueChanged.InvokeAsync(value);
            await NumberChanged.InvokeAsync(value);
        }
    }
}
