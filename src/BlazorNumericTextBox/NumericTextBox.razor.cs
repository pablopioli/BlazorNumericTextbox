using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorNumericTextBox
{
    public partial class NumericTextBox : ComponentBase
    {
        public static class Defaults
        {
            public static bool UseEnterAsTab { get; set; }
            public static string Class { get; set; } = "form-control";
            public static int MaxLength { get; set; } = 12;
            public static string DecimalSeparator { get; set; } = ".";
        }

        [Inject] IJSRuntime JsRuntime { get; set; }
        [Parameter] public string Id { get; set; }
        [Parameter] public string Class { get; set; } = Defaults.Class;
        [Parameter] public string Style { get; set; } = "";
        [Parameter] public int MaxLength { get; set; } = Defaults.MaxLength;
        [Parameter] public string Format { get; set; } = "";
        [Parameter] public decimal Value { get; set; } = 0;
        [Parameter] public string DecimalSeparator { get; set; } = Defaults.DecimalSeparator;
        [Parameter] public bool UseEnterAsTab { get; set; } = Defaults.UseEnterAsTab;
        [Parameter] public Func<decimal, string> ConditionalFormatting { get; set; }
        [Parameter] public EventCallback<decimal> ValueChanged { get; set; }

        private string VisibleValue = "";
        private string ActiveClass = "";

        private static Random Random = new Random();

        public NumericTextBox()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            Id = new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Next(s.Length)]).ToArray());

            ActiveClass = Class;
        }

        protected override void OnParametersSet()
        {
            if (string.IsNullOrEmpty(Format))
            {
                VisibleValue = Value.ToString();
            }
            else
            {
                VisibleValue = Value.ToString(Format);
            }
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
                        MaxLength.ToString()
                    });

                await JsRuntime.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });
            }
        }

        private void HasGotFocus()
        {
            ActiveClass = Class;
        }

        private async Task HasLostFocus()
        {
            var data = await JsRuntime.InvokeAsync<string>("GetNumericTextBoxValue", new string[] { "#" + Id });

            var cleaned = string.Join("",
                data.Replace("(", "-").Where(x => char.IsDigit(x) ||
                                             x == '-' ||
                                             x.ToString() == DecimalSeparator).ToArray());

            var parsed = decimal.TryParse(cleaned, out var valueAsDecimal);
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

            parsed = decimal.TryParse(cleaned, out var roundedValue);

            if (parsed)
            {
                Value = roundedValue;
            }
            else
            {
                Value = valueAsDecimal;
            }

            if (ConditionalFormatting == null)
            {
                ActiveClass = Class;
            }
            else
            {
                ActiveClass = Class + " " + ConditionalFormatting(Value);
            }

            await JsRuntime.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });
            await ValueChanged.InvokeAsync(Value);
        }
    }
}
