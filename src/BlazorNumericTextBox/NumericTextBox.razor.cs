using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BlazorNumericTextBox
{


    public partial class NumericTextBox<TItem> : ComponentBase
    {
        [CascadingParameter] EditContext EditContext { get; set; } = default;
        [Inject] IJSRuntime JsRuntime { get; set; }

        [Parameter] public string Id { get; set; }
        [Parameter] public string BaseClass { get; set; } = "form-control overflow-hidden";
        [Parameter] public string Class { get; set; }
        [Parameter] public string Style { get; set; } = "";
        [Parameter] public int MaxLength { get; set; } = NumericTextBoxDefaults.MaxLength;
        [Parameter] public string Format { get; set; } = "";

        [Parameter] public TItem PreviousValue { get; set; } = default(TItem);
        [Parameter] public TItem ValueBeforeFocus { get; set; } = default(TItem);
        [Parameter] public TItem Value { get; set; } = default(TItem);

        [Parameter] public bool UseEnterAsTab { get; set; } = NumericTextBoxDefaults.UseEnterAsTab;
        [Parameter] public bool SelectOnEntry { get; set; } = NumericTextBoxDefaults.SelectOnEntry;
        [Parameter] public CultureInfo Culture { get; set; }
        [Parameter] public Func<TItem, string> ConditionalFormatting { get; set; }
        [Parameter] public EventCallback<TItem> ValueChanged { get; set; }
        [Parameter] public EventCallback NumberChanged { get; set; }
        [Parameter] public Expression<Func<TItem>> ValueExpression { get; set; }

        private const string AlignToRight = "text-align:right;";
        private readonly string DecimalSeparator;

        private string VisibleValue = "";
        private string ActiveClass = "";
        private string ComputedStyle => AdditionalStyles + Style;
        private string AdditionalStyles = "";
        private FieldIdentifier FieldIdentifier;
        private IJSObjectReference JsModule;

        private static Random Random = new Random();

        public NumericTextBox()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            Id = new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Next(s.Length)]).ToArray());

            ActiveClass = ComputeClass();
            AdditionalStyles = AlignToRight;

            if (Culture == null)
            {
                if (CultureInfo.DefaultThreadCurrentUICulture != null)
                {
                    Culture = CultureInfo.DefaultThreadCurrentUICulture;
                }
                else
                {
                    Culture = NumericTextBoxDefaults.Culture;
                }
            }

            DecimalSeparator = Culture.NumberFormat.NumberDecimalSeparator;
        }

        private async Task SetVisibleValue(TItem value)
        {
            if (string.IsNullOrEmpty(Format))
            {
                VisibleValue = value.ToString();
            }
            else
            {
                VisibleValue = Convert.ToDecimal(value).ToString(Format);
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

            await JsModule.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            bool needUpdating;

            if (firstRender)
            {
                JsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorNumericTextBox/numerictextbox.js");

                string toDecimalSeparator = "";
                if (DecimalSeparator != ".")
                {
                    toDecimalSeparator = DecimalSeparator;
                }

                await JsModule.InvokeVoidAsync("ConfigureNumericTextBox",
                    new string[] {
                        "#" + Id,
                        ".",
                        toDecimalSeparator,
                        UseEnterAsTab ? "true" : "",
                        SelectOnEntry ? "true" : "",
                        MaxLength.ToString()
                    });

                await SetVisibleValue(Value);
                await JsModule.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });

                needUpdating = true;
            }
            else
            {
                needUpdating = !PreviousValue.Equals(Value);
            }

            if (needUpdating)
            {
                await SetVisibleValue(Value);
                await JsModule.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });
                PreviousValue = Value;
            }
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (EditContext != null)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                EditContext.OnValidationStateChanged += (sender, e) => StateHasChanged();
            }
        }

        private async Task HasGotFocus()
        {
            ValueBeforeFocus = Value;
            ActiveClass = ComputeClass();
            AdditionalStyles = "";

            decimal decValue = Convert.ToDecimal(Value);
            var value = decValue.ToString("G29", Culture.NumberFormat);
            await JsModule.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, value });

            if (decValue == 0)
            {
                await JsModule.InvokeVoidAsync("SelectNumericTextBoxContents", new string[] { "#" + Id, VisibleValue });
            }
        }

        private async Task HasLostFocus()
        {
            var data = await JsModule.InvokeAsync<string>("GetNumericTextBoxValue", new string[] { "#" + Id });

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
                Value = (TItem)Convert.ChangeType(roundedValue, typeof(TItem));
            }
            else
            {
                Value = (TItem)Convert.ChangeType(valueAsDecimal, typeof(TItem));
            }

            await SetVisibleValue(Value);

            await ValueChanged.InvokeAsync(Value);

            if (!ValueBeforeFocus.Equals(Value))
            {
                if (!string.IsNullOrEmpty(FieldIdentifier.FieldName))
                {
                    EditContext.NotifyFieldChanged(FieldIdentifier);
                }
                await NumberChanged.InvokeAsync(Value);
            }

            AdditionalStyles = AlignToRight;
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

        public async Task SetValue(TItem value)
        {
            Value = value;
            PreviousValue = value;
            await SetVisibleValue(value);
        }
    }
}
