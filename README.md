# BlazorNumericTextBox

## Summary

A Numeric TextBox for Blazor.

![Sample screenshot](sample.jpg "Sample screenshot")


### Main features

* Localizable
* Custom formatting
* Dynamic CSS classes
* Can set a MaxLength
* Copy and paste compatible

Available on Nuget as BlazorNumericTextBox.


### How to use it

In your _Imports.razor file add

```
@using BlazorNumericTextBox
```

Now you can add the input in any of your components

```
  <NumericTextBox></NumericTextBox>
```


Customize with:

* @bind-Value: Binds to a **decimal** value.
* Format: Format using any of standard numeric format strings.
* MaxLength: How many characters the user can enter.
* Class: CSS classes to add.
* BaseClass: The CSS class to use for the basic formatting, you need this because if has no styling on its own. By default assumes **form-control** from Bootstrap.
* Id: Customize the id attribute used, it you need to access it using Javascript interop.
* Style: Add a style tag to the output.
* ConditionalFormatting: You can provide a function to apply dynamic classes. In example: change the color for negative values.
* SelectOnEntry: When the textbox get focus all of it's contents are selected automatically.
* Culture: The culture to use in parsing and formatting the number. Defaults to the value of System.Globalizacion.CultureInfo.DefaultThreadCurrentUICulture.


You end with something like

```
<NumericTextBox @bind-Value="NumericProperty" Format="###,##0.00" Style="max-width:20em;"></NumericTextBox>
```

You can set global defaults for some of this values setting the appropiate values in the static class **NumericTextBoxDefaults**.
As an example, you could make all the numeric textboxes selected when they got focus including
**NumericTextBoxDefaults.SelectOnEntry = true**
in your program initialization.

Check the included sample to see it live.


### Notes for version 3

In version 3 a breaking change was introduced. Previously the control would capture the dot key and replace it to a comma for easier typing in countries where the comma is used as a decimal separator.

Now this feature is opt-in. For more information check
https://github.com/pablopioli/BlazorNumericTextbox/issues/4

