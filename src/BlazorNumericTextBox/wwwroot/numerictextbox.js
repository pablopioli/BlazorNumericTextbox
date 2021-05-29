export function GetNumericTextBoxValue(element) {
    return document.querySelector(element).innerHTML;
}

export function SetNumericTextBoxValue(element, value) {
    document.querySelector(element).innerHTML = value;
}

export function SelectNumericTextBoxContents(id) {
    var cell = document.querySelector(id);
    var range, selection;
    if (document.body.createTextRange) {
        range = document.body.createTextRange();
        range.moveToElementText(cell);
        range.select();
    } else if (window.getSelection) {
        selection = window.getSelection();
        range = document.createRange();
        range.selectNodeContents(cell);
        selection.removeAllRanges();
        selection.addRange(range);
    }
}

export function ConfigureNumericTextBox(element, source, to, selectOnEntry, maxLengthAsString, keyPressCustomFunction) {
    var maxLength = parseInt(maxLengthAsString);

    if (selectOnEntry) {
        document.querySelector(element).addEventListener('focus', function () {
            var cell = this;
            var range, selection;
            if (document.body.createTextRange) {
                range = document.body.createTextRange();
                range.moveToElementText(cell);
                range.select();
            } else if (window.getSelection) {
                selection = window.getSelection();
                range = document.createRange();
                range.selectNodeContents(cell);
                selection.removeAllRanges();
                selection.addRange(range);
            }
        });
    }

    document.querySelector(element).addEventListener("keypress", function (e) {
        // Workaround for Firefox
        var html = document.querySelector(element).innerHTML;
        if (html == "<br>") {
            document.querySelector(element).innerHTML = "";
            html = "";
        }

        if (html.length === maxLength) {
            e.preventDefault();
        }

        if (to !== "") {
            var transformedChar = transformTypedCharacter(e.key, source, to);
            if (transformedChar != e.key) {
                if (html.length !== maxLength) {
                    insertTextAtCursor(transformedChar);
                }

                e.preventDefault();
                return false;
            }
        }

        if (e.key === "-" || e.key == ".") {
            return true;
        }

        if (e.key >= "0" && e.key <= "9") {
            return true;
        }

        e.preventDefault();

        if (keyPressCustomFunction) {
            window[keyPressCustomFunction].apply(null, [e]);
        }

        return false;
    });

    document.querySelector(element).addEventListener("paste", function (e) {
        clipboardData = e.clipboardData || window.clipboardData;
        pastedData = clipboardData.getData('Text');

        if (pastedData) {
            var cleaned = "";
            for (var i = 0; i < pastedData.length; ++i) {

                if (pastedData[i] === "-" || (pastedData[i] >= "0" && pastedData[i] <= "9")) {
                    cleaned += pastedData[i];
                }

                if (to !== "" && pastedData[i] == source) {
                    cleaned += to;
                }
            }

            if (document.querySelector(element).innerHTML.length + cleaned.length >= maxLength) {
                e.preventDefault();
                return;
            }

            insertTextAtCursor(cleaned);
            e.preventDefault();
        }
        else {
            e.preventDefault();
        }
    });
}

export function transformTypedCharacter(typedChar, source, to) {
    return typedChar == source ? to : typedChar;
}

export function insertTextAtCursor(text) {
    var sel, range, textNode;
    if (window.getSelection) {
        sel = window.getSelection();
        if (sel.getRangeAt && sel.rangeCount) {
            range = sel.getRangeAt(0).cloneRange();
            range.deleteContents();
            textNode = document.createTextNode(text);
            range.insertNode(textNode);
            range.setStart(textNode, textNode.length);
            range.setEnd(textNode, textNode.length);
            sel.removeAllRanges();
            sel.addRange(range);
        }
    } else if (document.selection && document.selection.createRange) {
        range = document.selection.createRange();
        range.pasteHTML(text);
    }
}
