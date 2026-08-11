# HtmlTextBlock

A WPF `TextBlock` subclass (`AqiTechTips.HtmlTextBlock`) that renders a small HTML-like
markup subset directly as inline-formatted text — bold, italic, colors, links, custom
CSS-style properties — without needing a `FlowDocument` or `RichTextBox`.

```xml
<Window xmlns:ht="clr-namespace:AqiTechTips;assembly=HtmlTextBlock">
    <ht:HtmlTextBlock Html="The &lt;b&gt;quick&lt;/b&gt; &lt;i&gt;brown&lt;/i&gt; fox" />
</Window>
```

```csharp
myHtmlTextBlock.Html = "<span style=\"color:red;font-weight:bold\">Alert:</span> disk almost full";
```

## Install

NuGet package id: `AQI.HtmlTextBlock`. Targets `net10.0-windows7.0` (WPF).

## Important: this is inline-only

`HtmlTextBlock` is built on `TextBlock`/`Inlines`, so it only supports **inline text
styling** — bold, italic, color, links, etc. within a single flowing block of text.
It does **not** support block/layout markup: `<p>`, `<div>`, `<ul>/<li>`, `<table>`,
`<img>`, headings, or anything that would need its own box/line layout. Those tags are
recognized by the parser (so they won't break anything) but currently render nothing.
If you need real document layout, you want a `FlowDocument`/`RichTextBox`, not this
control.

## Two supported syntaxes

Real HTML angle brackets and the library's original `[tag]` square-bracket (BBCode-style)
syntax are both understood, auto-detected per tag — you can even mix them in the same
string. Use real HTML; the square-bracket form exists only for backward compatibility
with older content.

```
<b>bold</b>            same as            [b]bold[/b]
```

## Supported tags

| Tag | Effect |
|---|---|
| `<b>`, `<strong>` | Bold |
| `<i>`, `<em>` | Italic |
| `<u>` | Underline |
| `<s>`, `<strike>`, `<del>` | Strikethrough |
| `<sub>` | Subscript |
| `<sup>` | Superscript |
| `<mark>` | Yellow background highlight |
| `<code>` | Monospace font (Consolas) |
| `<small>` | ~85% of the `TextBlock`'s `FontSize` |
| `<big>` | ~120% of the `TextBlock`'s `FontSize` |
| `<font color="" face="" size="">` | Legacy color/family/size attributes |
| `<span style="...">` | Carries any combination of the CSS properties below |
| `<a href="...">` | Hyperlink (sets `Hyperlink.NavigateUri`) |
| `<br>` / `<br/>` | Line break |
| `<binding path="PropertyName">` | Binds to a property on the control's `DataContext` at render time |

Tags compose freely — `<b><span style="color:red">urgent</span></b>` is bold **and** red.

## `style="..."` — full custom styling

Any tag can carry a `style` attribute; the properties below are parsed and applied
regardless of which tag they're on (`<span>` is the natural choice for a plain wrapper):

| CSS property | Accepted values |
|---|---|
| `color` | named (`red`), hex (`#ff0000`), `rgb(r,g,b)` |
| `background-color` / `background` | same as `color` |
| `font-weight` | `bold`, `bolder`, or a number ≥ 600 |
| `font-style` | `italic`, `oblique` |
| `text-decoration` / `text-decoration-line` | `underline`, `line-through` (combinable: `"underline line-through"`) |
| `font-family` | any family name |
| `font-size` | `px`, `pt`, or a plain number |

```html
<span style="color:#3366ff;background-color:yellow;font-weight:bold;font-style:italic;text-decoration:underline;font-size:18px">
    fully custom
</span>
```

## HTML entities

Named (`&amp;`, `&lt;`, `&gt;`, `&quot;`, `&apos;`, `&nbsp;`, `&copy;`, `&reg;`, `&trade;`,
`&mdash;`, `&ndash;`, `&hellip;`, curly quotes) and numeric (`&#65;`, `&#x41;`) character
references are decoded in text content.

## Word-by-word styling — `HtmlTextBuilder`

For per-word effects (rainbow text, keyword highlighting, gradient emphasis) build the
markup with `HtmlTextBuilder.StyleWords` instead of hand-writing `<span>` tags:

```csharp
using AqiTechTips;

string[] palette = { "red", "orange", "gold", "green", "blue", "purple" };
string html = HtmlTextBuilder.StyleWords("The quick brown fox jumps over", (word, index) =>
    $"color:{palette[index % palette.Length]};font-weight:bold");

myHtmlTextBlock.Html = html;
```

Return `null` or `""` from the callback to leave a word unstyled:

```csharp
string html = HtmlTextBuilder.StyleWords(logLine, (word, index) =>
    word == "ERROR" ? "color:white;background-color:red;font-weight:bold" : null);
```

Notes:

- Whitespace between words is preserved exactly as it appeared in the input.
- Word text is HTML-escaped automatically, so words containing `&`, `<`, or `>` can't
  break the generated markup.
- The generated markup is plain text you can feed straight into `Html=`.

## `HtmlHighlightTextBlock`

A second control, `AqiTechTips.HtmlHighlightTextBlock`, adds a `Highlight` property:
any case-insensitive occurrence of that substring in `Html` gets wrapped in `<b>` before
rendering — a quick way to bold search-term matches.

```xml
<ht:HtmlHighlightTextBlock Html="{Binding Text}" Highlight="{Binding SearchTerm}" />
```

## More examples

```html
<!-- Mixed formatting -->
Status: <span style="color:green;font-weight:bold">OK</span> — last checked <i>2 minutes ago</i>

<!-- Strikethrough + replacement -->
<del>$40</del> <span style="color:red;font-weight:bold">$25</span>

<!-- Code snippet inline -->
Run <code>dotnet build</code> to compile.

<!-- Link -->
See the <a href="https://example.com/docs">docs</a> for details.

<!-- Highlight -->
Found <mark>3 matches</mark> for your search.

<!-- Data binding -->
Hello, <binding path="UserName" />!
```

## Testing

Tests live in `HtmlTextBlock.Tests` (xUnit), covering the parser (real HTML tags, legacy
`[x]` tags, mixed/auto-detected brackets, self-closing tags, comments, DOCTYPE, quoted
attribute parsing), rendering (every tag and style property above, hyperlinks, entity
decoding, nested tag composition), and `HtmlTextBuilder`. WPF `Inline`/`TextBlock`
assertions run on a dedicated STA thread (`StaThread.Run`) since they're
`DispatcherObject`s.

```bash
dotnet test HtmlTextBlock.Tests/HtmlTextBlock.Tests.csproj
```

## Performance

Parsing scans the input once with an index cursor (no re-slicing of the remaining text
per tag), and tag lookups use a cached dictionary rather than a linear scan — a
220KB/9000-tag document parses in roughly 0.15s.
