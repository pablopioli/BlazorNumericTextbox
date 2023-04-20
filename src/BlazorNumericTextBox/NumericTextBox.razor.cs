using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Globalization;
using System.Linq.Expressions;
using System.Text;

namespace BlazorNumericTextBox
{
    public partial class NumericTextBox<TItem> : ComponentBase
    {
        [CascadingParameter] EditContext? EditContext { get; set; } = default;
        [Inject] IJSRuntime JsRuntime { get; set; } = null!;

        [Parameter] public string Id { get; set; }
        [Parameter] public string BaseClass { get; set; } = "form-control overflow-hidden";
        [Parameter] public string Class { get; set; } = "";
        [Parameter] public string Style { get; set; } = "";
        [Parameter] public int MaxLength { get; set; } = NumericTextBoxDefaults.MaxLength;
        [Parameter] public string Format { get; set; } = "";
        [Parameter] public string KeyPressCustomFunction { get; set; } = "";
        [Parameter] public TItem? PreviousValue { get; set; } = default;
        [Parameter] public TItem? ValueBeforeFocus { get; set; } = default;
        [Parameter] public TItem? Value { get; set; } = default;
        [Parameter] public bool SelectOnEntry { get; set; } = NumericTextBoxDefaults.SelectOnEntry;
        [Parameter] public CultureInfo? Culture { get; set; }
        [Parameter] public Func<TItem, string>? ConditionalFormatting { get; set; }
        [Parameter] public EventCallback<TItem> ValueChanged { get; set; }
        [Parameter] public EventCallback NumberChanged { get; set; }
        [Parameter] public Expression<Func<TItem>>? ValueExpression { get; set; }
        [Parameter] public Func<Task>? OnFocus { get; set; }
        [Parameter] public Func<Task>? OnBlur { get; set; }
        [Parameter] public string CustomDecimalSeparator { get; set; } = "";

        [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

        private const string AlignToRight = "text-align:right;";

        private string VisibleValue = "";
        private string ActiveClass = "";
        private string ComputedStyle => AdditionalStyles + Style;
        private string AdditionalStyles = "";
        private FieldIdentifier FieldIdentifier;
        private IJSObjectReference? JsModule;

        private static readonly Random Random = new();

        public NumericTextBox()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";
            Id = new string(Enumerable.Repeat(chars, 12).Select(s => s[Random.Next(s.Length)]).ToArray());

            ActiveClass = ComputeClass();
            AdditionalStyles = AlignToRight;
        }

        private CultureInfo GetCulture()
        {
            if (Culture != null)
            {
                return Culture;
            }
            else if (CultureInfo.DefaultThreadCurrentUICulture != null)
            {
                return CultureInfo.DefaultThreadCurrentUICulture;
            }
            else
            {
                return NumericTextBoxDefaults.Culture;
            }
        }

        private async Task SetVisibleValue(TItem value)
        {
            var numberFormat = GetCulture().NumberFormat;

            if (string.IsNullOrEmpty(Format))
            {
                VisibleValue = Convert.ToDecimal(value).ToString("G", numberFormat) ?? "";
            }
            else
            {
                VisibleValue = Convert.ToDecimal(value).ToString(Format, numberFormat);
            }

            var additionalFormatting = string.Empty;
            if (ConditionalFormatting != null)
            {
                additionalFormatting = ConditionalFormatting(value);
            }

            ActiveClass = ComputeClass(additionalFormatting);

            if (JsModule != null)
            {
                var currentValue = await JsModule.InvokeAsync<string>("GetNumericTextBoxValue", new string[] { "#" + Id });
                if (currentValue != VisibleValue)
                {
                    await JsModule.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, VisibleValue });
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            bool needUpdating;

            if (firstRender)
            {
                JsModule = await JsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorNumericTextBox/numerictextbox.js");

                var toDecimalSeparator = NumericTextBoxDefaults.CustomDecimalSeparator;

                if (!string.IsNullOrEmpty(CustomDecimalSeparator))
                {
                    toDecimalSeparator = CustomDecimalSeparator;
                }

                await JsModule.InvokeVoidAsync("ConfigureNumericTextBox",
                    new string[] {
                        "#" + Id,
                        ".",
                        toDecimalSeparator,
                        SelectOnEntry ? "true" : "",
                        MaxLength.ToString(),
                        KeyPressCustomFunction
                    });

                if (Value != null)
                {
                    await SetVisibleValue(Value);
                }

                needUpdating = true;
            }
            else
            {
                if (PreviousValue != null)
                {
                    needUpdating = !PreviousValue.Equals(Value);
                }
                else
                {
                    needUpdating = true;
                }
            }

            if (needUpdating)
            {
                if (Value != null)
                {
                    await SetVisibleValue(Value);
                }

                PreviousValue = Value;
            }
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            ActiveClass = ComputeClass();

            if (EditContext != null && ValueExpression != null)
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
            var value = decValue.ToString("G29", GetCulture().NumberFormat);

            if (JsModule != null)
            {
                await JsModule.InvokeVoidAsync("SetNumericTextBoxValue", new string[] { "#" + Id, value });
            }

            if (decValue == 0 && JsModule != null)
            {
                await JsModule.InvokeVoidAsync("SelectNumericTextBoxContents", new string[] { "#" + Id, VisibleValue });
            }

            if (OnFocus != null)
            {
                await OnFocus.Invoke();
            }
        }

        private async Task HasLostFocus()
        {
            var culture = GetCulture();
            var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
            var numberFormat = culture.NumberFormat;

            var data = JsModule == null ? "" : await JsModule.InvokeAsync<string>("GetNumericTextBoxValue", new string[] { "#" + Id });

            var cleaned = string.Join("",
                data.Replace("(", "-").Where(x => char.IsDigit(x) ||
                                             x == '-' ||
                                             x.ToString() == decimalSeparator).ToArray());

            var parsed = decimal.TryParse(cleaned, NumberStyles.Any, numberFormat, out var valueAsDecimal);

            if (!parsed)
            {
                if (string.IsNullOrEmpty(Format))
                {
                    VisibleValue = 0.ToString("G", numberFormat);
                }
                else
                {
                    VisibleValue = 0.ToString(Format, numberFormat);
                }
            }
            else
            {                
                if (string.IsNullOrEmpty(Format))
                {
                    VisibleValue = valueAsDecimal.ToString("G", numberFormat);
                }
                else
                {
                    VisibleValue = valueAsDecimal.ToString(Format, numberFormat);
                }             
            }

            // Negative monetary values are represented with parenthesis
            cleaned = string.Join("",
                VisibleValue.Replace("(", "-")
                            .Where(x => char.IsDigit(x) ||
                                        x == '-' ||
                                        x.ToString() == decimalSeparator).ToArray());

            parsed = decimal.TryParse(cleaned, NumberStyles.Any, numberFormat, out var roundedValue);

            if (parsed)
            {
                Value = (TItem)Convert.ChangeType(roundedValue, typeof(TItem));
            }
            else
            {
                Value = (TItem)Convert.ChangeType(valueAsDecimal, typeof(TItem));
            }

            // Do not remove
            // Solves a problem in how Blazor changes the value of the Value property responding to browser events
            var value = Value;
            await SetVisibleValue(Value);
            Value = value;
            await ValueChanged.InvokeAsync(Value);

            if (PreviousValue != null && !PreviousValue.Equals(Value))
            {
                if (!string.IsNullOrEmpty(FieldIdentifier.FieldName))
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }
                await NumberChanged.InvokeAsync(Value);
            }

            AdditionalStyles = AlignToRight;

            if (OnBlur != null)
            {
                await OnBlur.Invoke();
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

        public async Task SetValue(TItem value)
        {
            Value = value;
            PreviousValue = value;
            await SetVisibleValue(value);
        }
    }
}
