# Reusable Patterns — KnowledgeTrackerDesktop

This document collects the techniques built in this application that can be
carried over to another Windows desktop application: how a custom Syncfusion
dark theme is applied, how the schema is migrated without ever losing data,
how a language is switched at runtime, how a WinForms UI is tested without
opening a window, and so on.

Each pattern states:

- **What it does** — the problem it solves.
- **Where it lives** — the files in this repository.
- **Dependencies** — what has to be present for it to work.
- **Code** — the excerpt, copied from the current sources.
- **Pitfalls** — the issues that were already paid for here.

Nothing in this document is a framework. Each section is small enough to be
copied into another project and adapted. The code is the source of truth; this
document explains *why* it looks the way it does.

## Conventions used in the excerpts

- `using` directives are omitted unless they carry information.
- `CarbonPalette`, `AppTheme` and `ButtonRole` live in
  `KnowledgeTrackerDesktop.UI` / `KnowledgeTrackerDesktop.UI.Theme`.
- Namespaces are shown as they are in this repository. When copying a section
  into another project, rename the namespaces to that project's.

## Prerequisites

| Requirement | Version used here | Notes |
|---|---|---|
| Windows | Windows 10 1809+ | The app targets `net10.0-windows` with `UseWindowsForms`. |
| .NET | 10.0 | SDK 10.0.402 was used for the build. |
| Syncfusion WinForms | 35.1.39 | Only sections A, B (partly), E and G need it. A Community License is enough. |
| sqlite-net | 1.9.172 | `SQLiteAsyncConnection`, `CreateTableAsync<T>`, `RunInTransactionAsync`. |
| Markdig | 0.37.0 | Currently only used by the Help dialog. |

---

# A. Visual identity and theming

## A1. Applying a custom Syncfusion dark theme

**What it does.** Makes an entire WinForms tree dark, including Syncfusion
controls, legacy Syncfusion controls that predate the theming engine, and
custom-drawn controls.

**Where it lives.** `UI/Theme/CarbonTheme.cs`, `UI/Theme/Themes/CarbonDark/*.xml`.

**Dependencies.** Syncfusion WinForms; two embedded theme XML files named
`CarbonDark`.

### The strategy

Syncfusion has two generations of controls, and they do not theme the same
way. The working split is:

1. A **built-in dark visual style** is applied to the whole tree. Legacy
   controls only accept `VisualTheme` enum names, so the custom theme name
   must never be set on them (or on a container that owns them).
2. **Modern controls** (`SfButton`, `SfDateTimeEdit`, `SfNumericTextBox`)
   accept an arbitrary theme name, so they are switched to the embedded
   `CarbonDark` theme.
3. **Legacy controls** (`TextBoxExt`, `ComboBoxAdv`, `CheckBoxAdv`) cannot load
   a custom theme name, so their colours are set explicitly in code.

### The entry point

```csharp
internal static class CarbonTheme
{
    private static readonly object Gate = new();
    private static readonly ConditionalWeakTable<ComboBoxAdv, object> ComboBoxHandleHooks = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (Gate)
        {
            if (_initialized)
            {
                return;
            }

            SfSkinManager.LoadAssembly(typeof(CarbonTheme).Assembly);
            SfSkinManager.LoadAssembly(typeof(Syncfusion.WinForms.Themes.Office2016Theme).Assembly);
            _initialized = true;
        }
    }

    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Initialize();

        SfSkinManager.SetTheme(root, CarbonPalette.BaseVisualStyle);

        if (root is Form form)
        {
            form.BackColor = CarbonPalette.DeepBackground;
            form.ForeColor = CarbonPalette.TextPrimary;

            if (form is SfForm sfForm)
            {
                sfForm.AllowRoundedCorners = true;
            }
        }

        ApplyOverrides(root);
    }
```

`Apply` is idempotent and safe to call on the shell and again on each view as
it is shown:

```csharp
// frmMain.cs
CarbonTheme.Apply(this);      // once, for the shell
CarbonTheme.Apply(view);      // again, for each hosted view
```

### The per-control overrides

```csharp
    private static void ApplyOverrides(Control control)
    {
        switch (control)
        {
            case GradientPanelExt gradientPanel:
                gradientPanel.CornerRadius = CarbonPalette.CornerRadius;
                break;
            case SfButton button:
                // Modern control: accepts the custom theme name. Setting the
                // theme name resets Style, so the semantic role is applied
                // afterwards (or a neutral secondary look by default).
                button.ThemeName = CarbonPalette.ThemeName;
                button.Font = AppTheme.Fonts.Body;
                if (ButtonStyler.TryGetRole(button, out _))
                {
                    ButtonStyler.Reapply(button);
                }
                else
                {
                    ButtonStyler.Apply(button, ButtonRole.Secondary);
                }
                break;
            case SfDateTimeEdit dateTimeEdit:
                dateTimeEdit.ThemeName = CarbonPalette.ThemeName;
                dateTimeEdit.BackColor = CarbonPalette.DeepBackground;
                dateTimeEdit.ForeColor = CarbonPalette.TextPrimary;
                dateTimeEdit.Font = AppTheme.Fonts.Input;
                break;
            case TextBoxExt textBox:
                ApplyTextBoxExt(textBox);
                break;
            case ComboBoxAdv comboBox:
                ApplyComboBoxAdv(comboBox);
                break;
            case CheckBoxAdv checkBox:
                ApplyCheckBoxAdv(checkBox);
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyOverrides(child);
        }
    }
```

### The combo box that only looks right after its handle exists

```csharp
    private static void ApplyComboBoxAdv(ComboBoxAdv comboBox)
    {
        comboBox.CanOverrideStyle = true;
        comboBox.UseBackColor = true;
        if (!ComboBoxHandleHooks.TryGetValue(comboBox, out _))
        {
            ComboBoxHandleHooks.Add(comboBox, new object());
            comboBox.HandleCreated += (_, _) => ApplyComboBoxAdvVisuals(comboBox);
        }

        ApplyComboBoxAdvVisuals(comboBox);
    }

    private static void ApplyComboBoxAdvVisuals(ComboBoxAdv comboBox)
    {
        comboBox.BackColor = CarbonPalette.DeepBackground;
        comboBox.ForeColor = CarbonPalette.TextPrimary;
        comboBox.Font = AppTheme.Fonts.Input;
        comboBox.FlatBorderColor = CarbonPalette.Border;
        comboBox.ThemeStyle.EditorStyle.BackColor = CarbonPalette.DeepBackground;
        comboBox.ThemeStyle.EditorStyle.TextColor = CarbonPalette.TextPrimary;
        comboBox.ThemeStyle.EditorStyle.BorderColor = CarbonPalette.Border;
        comboBox.ThemeStyle.EditorStyle.FocusedBorderColor = CarbonPalette.Accent;
        comboBox.ThemeStyle.EditorStyle.HoverBorderColor = CarbonPalette.AccentAlt;

        comboBox.ThemeStyle.DropDownStyle.ItemBackColor = CarbonPalette.Surface;
        comboBox.ThemeStyle.DropDownStyle.ItemForeColor = CarbonPalette.TextPrimary;
        comboBox.ThemeStyle.DropDownStyle.SelectedItemBackColor = CarbonPalette.Accent;
        comboBox.ThemeStyle.DropDownStyle.SelectedItemForeColor = CarbonPalette.TextPrimary;
        comboBox.ThemeStyle.DropDownStyle.BorderColor = CarbonPalette.Border;

        comboBox.ThemeStyle.DropDownButtonStyle.BackColor = CarbonPalette.Surface;
        comboBox.ThemeStyle.DropDownButtonStyle.HoverBackColor = CarbonPalette.Border;
        comboBox.ThemeStyle.DropDownButtonStyle.PressedBackColor = CarbonPalette.Accent;
        comboBox.ThemeStyle.DropDownButtonStyle.BorderColor = CarbonPalette.Border;
        comboBox.ThemeStyle.DropDownButtonStyle.HoverBorderColor = CarbonPalette.AccentAlt;
        comboBox.ThemeStyle.DropDownButtonStyle.PressedBorderColor = CarbonPalette.Accent;
        comboBox.ThemeStyle.DropDownButtonStyle.ArrowColor = CarbonPalette.TextPrimary;
        comboBox.ThemeStyle.DropDownButtonStyle.ArrowHoverColor = CarbonPalette.TextPrimary;
        comboBox.ThemeStyle.DropDownButtonStyle.ArrowPressedColor = CarbonPalette.TextPrimary;

        comboBox.ListBox.BackColor = CarbonPalette.Surface;
        comboBox.ListBox.ForeColor = CarbonPalette.TextPrimary;
        comboBox.ListBox.Font = AppTheme.Fonts.Input;
    }
```

### Pitfalls

- **Assigning `ThemeName` resets `Style`.** A button that had a role before the
  theme was applied loses it. Apply the theme first, then the role — or use
  `ButtonStyler.Reapply`, which is why roles are remembered (see A2).
- **`ComboBoxAdv` re-creates its list box when the handle is created.** The
  colours applied before that point are lost, hence the `HandleCreated` hook.
  `ConditionalWeakTable` keeps the hook from leaking the control.
- **Never set the custom theme name on a legacy control or on a container
  that owns legacy controls.** They only understand `VisualTheme` enum names
  and will silently fall back to the light theme.
- **A dark theme is not enough on its own.** Set `form.ForeColor` explicitly;
  otherwise labels inherit the system colour and stay black on black.

---

## A2. Semantic button roles

**What it does.** Keeps button colours out of the views: a button declares
*what it means* (`Primary`, `Danger`, `Ghost`, `Nav`, `Icon`, …) and the
theme decides how it looks.

**Where it lives.** `UI/Theme/ButtonStyler.cs`, `UI/Theme/ButtonRole.cs`.

**Dependencies.** Syncfusion `SfButton`; `CarbonPalette`.

### The roles

```csharp
public enum ButtonRole
{
    Primary,
    Secondary,
    Ghost,
    Danger,
    Nav,
    NavActive,
    Icon,
    IconDanger,
}
```

### Remembering the role so it can be re-applied

```csharp
internal static class ButtonStyler
{
    private sealed class RoleRegistration
    {
        public RoleRegistration(ButtonRole role) => Role = role;

        public ButtonRole Role { get; set; }
    }

    private static readonly ConditionalWeakTable<SfButton, RoleRegistration> Registrations = new();

    public static void Apply(SfButton button, ButtonRole role)
    {
        ArgumentNullException.ThrowIfNull(button);

        if (Registrations.TryGetValue(button, out var existing))
        {
            existing.Role = role;
        }
        else
        {
            Registrations.Add(button, new RoleRegistration(role));
        }

        ApplyRole(button, role);
    }

    public static bool TryGetRole(SfButton button, out ButtonRole role)
    {
        ArgumentNullException.ThrowIfNull(button);

        if (Registrations.TryGetValue(button, out var registration))
        {
            role = registration.Role;
            return true;
        }

        role = default;
        return false;
    }

    /// <summary>
    /// Re-applies the registered role, if any. Used by <see cref="CarbonTheme"/>
    /// after the Syncfusion skin has reset the button style.
    /// </summary>
    public static void Reapply(SfButton button)
    {
        ArgumentNullException.ThrowIfNull(button);

        if (button.IsDisposed)
        {
            return;
        }

        if (TryGetRole(button, out var role))
        {
            ApplyRole(button, role);
        }
    }
}
```

### Applying one role

```csharp
    private static void ApplyRole(SfButton button, ButtonRole role)
    {
        button.Font = AppTheme.Fonts.Body;

        switch (role)
        {
            case ButtonRole.Primary:
                Fill(button, CarbonPalette.Primary, CarbonPalette.PrimaryHover, CarbonPalette.PrimaryPressed);
                break;
            case ButtonRole.Danger:
                Fill(button, CarbonPalette.Danger, CarbonPalette.DangerHover, CarbonPalette.DangerPressed);
                break;
            case ButtonRole.Secondary:
                Outlined(button);
                break;
            case ButtonRole.Ghost:
                Ghost(button);
                break;
            case ButtonRole.Icon:
                Icon(button);
                break;
            case ButtonRole.IconDanger:
                IconDanger(button);
                break;
            // Nav / NavActive handled below.
        }
    }

    private static void Fill(SfButton button, Color back, Color hover, Color pressed)
    {
        var style = button.Style;

        style.BackColor = back;
        style.ForeColor = CarbonPalette.TextPrimary;
        style.HoverBackColor = hover;
        style.HoverForeColor = CarbonPalette.TextPrimary;
        style.PressedBackColor = pressed;
        style.PressedForeColor = CarbonPalette.TextPrimary;
        style.FocusedBackColor = back;
        style.FocusedForeColor = CarbonPalette.TextPrimary;
        style.DisabledBackColor = CarbonPalette.DisabledBack;
        style.DisabledForeColor = CarbonPalette.DisabledText;

        style.Border = null;
        style.HoverBorder = null;
        style.PressedBorder = null;
        style.FocusedBorder = null;
        style.DisabledBorder = null;
    }

    private static void Outlined(SfButton button)
    {
        var style = button.Style;

        style.BackColor = CarbonPalette.Surface;
        style.ForeColor = CarbonPalette.TextPrimary;
        style.HoverBackColor = CarbonPalette.SecondaryHover;
        style.HoverForeColor = CarbonPalette.TextPrimary;
        style.PressedBackColor = CarbonPalette.SecondaryPressed;
        style.PressedForeColor = CarbonPalette.TextPrimary;
        style.FocusedBackColor = CarbonPalette.Surface;
        style.FocusedForeColor = CarbonPalette.TextPrimary;
        style.DisabledBackColor = CarbonPalette.DisabledBack;
        style.DisabledForeColor = CarbonPalette.DisabledText;

        style.Border = BorderPen(CarbonPalette.Border);
        style.HoverBorder = BorderPen(CarbonPalette.SecondaryBorderHover);
        style.PressedBorder = BorderPen(CarbonPalette.Border);
        style.FocusedBorder = BorderPen(CarbonPalette.AccentAlt);
        style.DisabledBorder = BorderPen(CarbonPalette.Border);
    }
```

### A flat WinForms button, for controls that are not Syncfusion

```csharp
    public static void ApplyFlat(Button button, ButtonRole role)
    {
        ArgumentNullException.ThrowIfNull(button);

        var filled = role is ButtonRole.Primary or ButtonRole.Danger;
        var accent = role == ButtonRole.Danger ? CarbonPalette.Danger : CarbonPalette.Primary;
        var hover = role == ButtonRole.Danger ? CarbonPalette.DangerHover : CarbonPalette.PrimaryHover;
        var pressed = role == ButtonRole.Danger ? CarbonPalette.DangerPressed : CarbonPalette.PrimaryPressed;

        button.FlatStyle = FlatStyle.Flat;
        button.UseVisualStyleBackColor = false;
        button.BackColor = filled ? accent : CarbonPalette.Surface;
        button.ForeColor = CarbonPalette.TextPrimary;
        button.FlatAppearance.BorderColor = filled ? accent : CarbonPalette.Border;
        button.FlatAppearance.BorderSize = filled ? 2 : 1;
        button.FlatAppearance.MouseOverBackColor = filled ? hover : CarbonPalette.SecondaryHover;
        button.FlatAppearance.MouseDownBackColor = filled ? pressed : CarbonPalette.SecondaryPressed;
    }
```

### Pitfalls

- **Never share a `Pen` between buttons.** Syncfusion takes ownership of
  `Style.Border` and disposes the previous value whenever one is reassigned.
  Sharing one instance leaves the other buttons holding a disposed pen, which
  crashes the paint. Always create a fresh pen per assignment:

  ```csharp
  private static Pen BorderPen(Color color) => new(color, 1f);
  ```

- **`SfButton` does not honour `Color.Transparent`.** It falls back to the
  control's light surface. To fake transparency, match the parent's
  background colour:

  ```csharp
  private static Color ResolveIconBackground(SfButton button)
  {
      var parent = button.Parent?.BackColor;
      return parent is { } color && color.A == 255 && IsDark(color)
          ? color
          : AppTheme.Background;
  }

  private static bool IsDark(Color color) =>
      ((0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B)) < 128;
  ```

---

## A3. Rounded corners

**What it does.** Rounds panels and surfaces without clipping their content.

**Where it lives.** `UI/Theme/ModernLayoutHelper.cs`.

**Dependencies.** `Syncfusion.Windows.Forms.Tools.GradientPanelExt`.

### The rounded rectangle path

```csharp
    public static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return path;
        }

        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        int r = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
        int diameter = r * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270f, 90f);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();
        return path;
    }
```

### Clipping a control to the rounded shape, and keeping it on resize

```csharp
    public static void ApplyRoundedRegion(Control control, int radius = CarbonPalette.CornerRadius)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (radius <= 0)
        {
            return;
        }

        if (!control.IsDisposed)
        {
            if (RegionBindings.TryGetValue(control, out var binding))
            {
                binding.Radius = radius;
            }
            else
            {
                RegionBindings.Add(control, new RegionBinding(control, radius));
            }
        }

        ApplyRegion(control, radius);
    }

    private static void ApplyRegion(Control control, int radius)
    {
        if (control.IsDisposed)
        {
            return;
        }

        Size size = control.ClientSize;
        if (size.Width < 1 || size.Height < 1)
        {
            return;
        }

        int r = Math.Max(1, Math.Min(radius, Math.Min(size.Width, size.Height) / 2));
        using GraphicsPath path = RoundedPath(new Rectangle(Point.Empty, size), r);
        control.Region = new Region(path);
    }
```

The `Resize` subscription that keeps the region in sync:

```csharp
    private sealed class RegionBinding
    {
        public RegionBinding(Control control, int radius)
        {
            Radius = radius;
            control.Resize += OnResize;
        }

        public int Radius { get; set; }

        private void OnResize(object? sender, EventArgs e)
        {
            if (sender is Control control)
            {
                ApplyRegion(control, Radius);
            }
        }
    }

    private static readonly ConditionalWeakTable<Control, RegionBinding> RegionBindings = new();
```

### Wrapping a compound input in a rounded border

A `ComboBoxAdv` or `SfDateTimeEdit` cannot be rounded directly — clipping it
cuts the text area. Wrapping it in a rounded panel gives the same look with
no clipping:

```csharp
    public static GradientPanelExt WrapInRoundedPanel(
        Control input,
        int radius = CarbonPalette.CornerRadius,
        int height = 32,
        Color? borderColor = null)
    {
        ArgumentNullException.ThrowIfNull(input);

        Color border = borderColor ?? CarbonPalette.Border;
        var wrapper = new GradientPanelExt
        {
            AutoSize = false,
            Height = height,
            CornerRadius = Math.Max(2, radius),
            BorderGap = 0,
            InnerBorderColor = border,
            InnerBorderThickness = 1,
            BackColor = CarbonPalette.DeepBackground,
            Padding = new Padding(radius, 1, radius, 1),
            Margin = new Padding(0, 4, 8, 4),
        };

        input.Dock = DockStyle.Fill;
        input.Margin = Padding.Empty;
        wrapper.Controls.Add(input);
        return wrapper;
    }
```

### Pitfalls

- **Do not round generic layout controls or inputs.** Their regions clip
  content and cause redraw flicker. Only round explicit surfaces (cards) and
  native `GradientPanelExt` wrappers.
- **Always dispose the `GraphicsPath`** — it holds unmanaged GDI+ handles.
  The helper uses `using` for that reason.
- **The radius must be clamped** to half of the smaller side, otherwise the
  arcs overlap and the path renders incorrectly.

---

## A4. Design tokens

**What it does.** Keeps every colour in one place, so the whole UI stays
consistent and a theme change is a single edit.

**Where it lives.** `UI/Theme/CarbonPalette.cs` (raw palette) and
`UI/AppTheme.cs` (semantic layer + typography).

**Dependencies.** None.

### The raw palette

```csharp
internal static class CarbonPalette
{
    /// <summary>Name of the custom Syncfusion theme (embedded XML resources).</summary>
    public const string ThemeName = "CarbonDark";

    /// <summary>
    /// Built-in dark visual style used as the base. Syncfusion legacy
    /// controls only accept <c>VisualTheme</c> enum names, so the custom
    /// <see cref="ThemeName"/> cannot be applied to them; the base style
    /// keeps them dark and the code-level overrides refine their colours.
    /// </summary>
    public const string BaseVisualStyle = "Office2016DarkGray";

    public static readonly Color DeepBackground = ColorTranslator.FromHtml("#121824");
    public static readonly Color Surface = ColorTranslator.FromHtml("#1E2640");
    public static readonly Color Accent = ColorTranslator.FromHtml("#7C3AED");
    public static readonly Color AccentAlt = ColorTranslator.FromHtml("#06B6D4");
    public static readonly Color Success = ColorTranslator.FromHtml("#22C55E");
    public static readonly Color TextPrimary = ColorTranslator.FromHtml("#F8FAFC");
    public static readonly Color TextSecondary = ColorTranslator.FromHtml("#94A3B8");
    public static readonly Color Border = ColorTranslator.FromHtml("#2E375E");

    public static readonly Color Primary = ColorTranslator.FromHtml("#427792");
    public static readonly Color PrimaryHover = ColorTranslator.FromHtml("#4E86A0");
    public static readonly Color PrimaryPressed = ColorTranslator.FromHtml("#345F76");
    public static readonly Color Danger = ColorTranslator.FromHtml("#9E5B57");
    public static readonly Color DangerHover = ColorTranslator.FromHtml("#B06D69");
    public static readonly Color DangerPressed = ColorTranslator.FromHtml("#7C4642");

    /// <summary>Standard corner radius used where a control exposes one.</summary>
    public const int CornerRadius = 8;
}
```

### The semantic layer

Views ask for a *meaning*, never for a colour:

```csharp
internal static class AppTheme
{
    /// <summary>General canvas / form background.</summary>
    public static Color Background => CarbonPalette.DeepBackground;

    /// <summary>Panels, cards, sidebars and header surfaces.</summary>
    public static Color Surface => CarbonPalette.Surface;

    public static Color CardBorder => CarbonPalette.Border;
    public static Color CardSelectedBorder => CarbonPalette.Accent;
    public static Color PrimaryText => CarbonPalette.TextPrimary;
    public static Color SecondaryText => CarbonPalette.TextSecondary;
    public static Color Accent => CarbonPalette.Accent;
    public static Color Success => CarbonPalette.Success;
    public static Color Error => Color.FromArgb(248, 113, 113);

    /// <summary>
    /// Shared typography. The application body font is installed before
    /// controls are created; views opt into larger role fonts as needed.
    /// </summary>
    public static class Fonts
    {
        public static Font Body { get; } = new("Segoe UI", 12f);
        public static Font Input { get; } = new("Segoe UI", 13f);
        public static Font CardTitle { get; } = new("Segoe UI", 14f, FontStyle.Bold);
        public static Font CardHeader { get; } = new("Segoe UI", 11f, FontStyle.Bold);
        public static Font SectionTitle { get; } = new("Segoe UI", 12f, FontStyle.Bold);
        public static Font Caption { get; } = new("Segoe UI", 11f);
        public static Font Small { get; } = new("Segoe UI", 10f);

        public static void RegisterAsDefault()
        {
            Application.SetDefaultFont(Body);
        }
    }
}
```

### Pitfalls

- **`AppTheme.Fonts` are static singletons.** A `Font` is a GDI+ object with a
  lifetime; do not dispose them, and do not create one per control.
- **Register the default font before creating any control**, otherwise the
  first controls keep the system font:
  `AppTheme.Fonts.RegisterAsDefault();` in `Program.Main`, right after
  `ApplicationConfiguration.Initialize()`.

---

## A5. Custom-drawn, theme-aware controls (GDI+)

**What it does.** Renders a chart without a third-party charting dependency,
fully consistent with the palette.

**Where it lives.** `UI/Views/ActivityChart.cs` (column chart),
`UI/Views/KpiCard.cs`, `UI/Views/StatListRow.cs`.

**Dependencies.** `System.Drawing`; `AppTheme` for the colours.

### The control skeleton

```csharp
internal sealed class ActivityChart : Control
{
    private IReadOnlyList<MonthCount> _months = Array.Empty<MonthCount>();

    public ActivityChart()
    {
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
        BackColor = AppTheme.Surface;
    }

    public void SetMonths(IReadOnlyList<MonthCount> months)
    {
        _months = months ?? Array.Empty<MonthCount>();
        Invalidate();
    }
}
```

`ControlStyles.OptimizedDoubleBuffer` and `ResizeRedraw` are what make the
control flicker-free.

### The painting

```csharp
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        var plot = PlotArea();
        if (_months.Count == 0 || plot.Width <= 0 || plot.Height <= 0)
        {
            return;
        }

        int max = _months.Max(month => month.Count);
        if (max <= 0)
        {
            return;
        }

        int axisMax = NiceMax(max);
        DrawGrid(g, plot, axisMax);

        float slot = (float)plot.Width / _months.Count;
        float barWidth = Math.Min(slot * 0.62f, 44f);
        using var valueBrush = new SolidBrush(AppTheme.SecondaryText);

        for (int i = 0; i < _months.Count; i++)
        {
            int count = _months[i].Count;
            if (count <= 0)
            {
                continue;
            }

            float height = (float)count / axisMax * plot.Height;
            float x = plot.Left + (slot * i) + ((slot - barWidth) / 2f);
            float y = plot.Bottom - height;
            var rect = new RectangleF(x, y, barWidth, height);

            using var brush = new LinearGradientBrush(
                rect,
                AppTheme.Accent,
                AppTheme.AccentAlt,
                LinearGradientMode.Vertical);
            using GraphicsPath barPath = RoundedTop(rect, Math.Min(5f, barWidth / 2f));
            g.FillPath(brush, barPath);

            string label = count.ToString();
            var size = g.MeasureString(label, AppTheme.Fonts.Small);
            if (barWidth >= size.Width + 2f)
            {
                g.DrawString(
                    label,
                    AppTheme.Fonts.Small,
                    valueBrush,
                    x + ((barWidth - size.Width) / 2f),
                    y - size.Height - 1f);
            }
        }

        DrawMonthLabels(g, plot, slot);
    }
```

### A "nice" axis maximum

```csharp
    private static int NiceMax(int max)
    {
        if (max <= GridSteps)
        {
            return max;
        }

        double power = Math.Pow(10, Math.Floor(Math.Log10(max)));
        foreach (double multiple in new[] { 1d, 2d, 2.5d, 5d, 10d })
        {
            double candidate = multiple * power;
            if (candidate >= max)
            {
                return (int)Math.Ceiling(candidate);
            }
        }

        return max;
    }
```

### Pitfalls

- **Always `using` the brushes and paths.** GDI+ objects are not garbage
  collected promptly; a chart that repaints on every mouse move will exhaust
  GDI handles without this.
- **Guard against a zero-sized control.** `OnPaint` can run before the
  control has a size; dividing by zero there crashes the app.
- **Format dates with the active culture**, so month labels follow the
  language:
  `new DateTime(month.Year, month.Month, 1).ToString("MMM", AppLocalization.Culture)`.

---

# B. Localization

## B1. Switching the language at runtime, without a restart

**What it does.** Translates the whole visible UI when the user picks another
language, and remembers the choice.

**Where it lives.** `UI/AppLocalization.cs`.

**Dependencies.** The translation dictionaries (one per language).

### The model

The English source string is the key. `Translate` returns the original text
when there is no translation, so a missing entry degrades to English instead
of showing a key.

```csharp
internal enum AppLanguage
{
    English,
    Portuguese,
}

internal static class AppLocalization
{
    private const string EnglishCulture = "en-US";
    private const string PortugueseCulture = "pt-PT";

    private static readonly Dictionary<string, string> PortugueseText = new(StringComparer.Ordinal)
    {
        ["Library"] = "Biblioteca",
        ["Statistics"] = "Estatísticas",
        ["Save"] = "Guardar",
        ["Cancel"] = "Cancelar",
        // ...
    };

    public static AppLanguage Language { get; private set; } = AppLanguage.English;
    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo(EnglishCulture);
    public static event EventHandler? LanguageChanged;
}
```

### Reading the saved preference at startup

```csharp
    public static void Initialize()
    {
        var selectedCulture = EnglishCulture;
        try
        {
            if (File.Exists(SettingsPath))
            {
                var persistedCulture = File.ReadAllText(SettingsPath).Trim();
                if (persistedCulture is EnglishCulture or PortugueseCulture)
                {
                    selectedCulture = persistedCulture;
                }
                else
                {
                    Debug.WriteLine($"Ignoring unsupported saved language '{persistedCulture}'.");
                }
            }
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"Unable to read the saved language preference: {ex}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Debug.WriteLine($"Unable to read the saved language preference: {ex}");
        }

        ApplyCulture(selectedCulture);
    }
```

### Switching and persisting

```csharp
    public static void SetLanguage(AppLanguage language)
    {
        var cultureName = GetCulture(language).Name;

        if (Language == language)
        {
            return;
        }

        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException("The language settings path has no parent directory.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(SettingsPath, cultureName);
        ApplyCulture(cultureName);
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    private static void ApplyCulture(string cultureName)
    {
        Culture = CultureInfo.GetCultureInfo(cultureName);
        Language = cultureName == PortugueseCulture ? AppLanguage.Portuguese : AppLanguage.English;
        CultureInfo.DefaultThreadCurrentCulture = Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Culture;
        CultureInfo.CurrentCulture = Culture;
        CultureInfo.CurrentUICulture = Culture;
    }
```

### Binding a control

The English text is stored in `Tag` and the translated text in `Text`. That
is what makes the whole tree translatable later without keeping a reference
to every control:

```csharp
    public static void Bind(Control control, string englishText)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.Tag = englishText;
        control.Text = Translate(englishText);
    }

    public static void Bind(ToolStripItem item, string englishText)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Tag = englishText;
        item.Text = Translate(englishText);
    }

    public static void BindComboItems(ComboBoxAdv comboBox, params (int Index, string EnglishText)[] items)
    {
        ArgumentNullException.ThrowIfNull(comboBox);
        comboBox.Tag = items;
        ApplyComboItems(comboBox, items);
    }
```

### Re-applying the translation to the whole tree

```csharp
    public static void Apply(Control root)
    {
        if (root.Tag is string key && PortugueseText.ContainsKey(key))
        {
            root.Text = Translate(key);
        }
        else if (root.Tag is Func<string> textFactory)
        {
            root.Text = textFactory();
        }

        if (root.ContextMenuStrip is not null)
        {
            Apply(root.ContextMenuStrip.Items);
        }

        if (root is SfDateTimeEdit dateTimeEdit)
        {
            dateTimeEdit.Watermark = Translate("Not set");
        }

        if (root is ComboBoxAdv comboBoxWithItems && root.Tag is ValueTuple<int, string>[] items)
        {
            ApplyComboItems(comboBoxWithItems, items);
        }

        foreach (Control child in root.Controls)
        {
            Apply(child);
        }
    }
```

### Pitfalls

- **`Tag` is a shared slot.** If a control needs `Tag` for something else,
  use the `Func<string>` overload, which stores a factory instead of a string.
- **Menu items live outside `Controls`.** A `ContextMenuStrip` has to be
  walked separately — see the `ToolStripItemCollection` overload.
- **A missing translation is silent.** With the source string as key, a typo
  in the view and a typo in the dictionary cancel out and the UI stays in
  English with no error. For a larger app, load the dictionaries from JSON or
  `.resx` and log the misses.
- **Setting `Culture` is not enough.** Date and number formatting only follow
  the application if `DefaultThreadCurrentCulture` and `CurrentCulture` are
  set as well (see `ApplyCulture`).
- **Remember the paths of the packaged app.** `LocalApplicationData` is
  redirected to the package-private folder for MSIX builds, so the preference
  file lands in the app's private storage automatically.

---

# C. Persistence

The schema has no foreign keys and no cascade: referential integrity is the
application's responsibility. Every pattern below follows from that.

## C1. Versioned schema migrations

**What it does.** Creates and upgrades the database without losing data, and
without ever leaving it half-migrated.

**Where it lives.** `Data/SchemaMigrator.cs`.

**Dependencies.** sqlite-net (`SQLiteAsyncConnection`).

```csharp
public sealed class SchemaMigrator
{
    /// <summary>
    /// Version written to PRAGMA user_version.
    /// Increment only when a real schema change is introduced together with
    /// its migration and tests.
    /// </summary>
    public const int CurrentVersion = 1;

    private readonly DatabasePath _databasePath;

    public SchemaMigrator(DatabasePath databasePath)
    {
        _databasePath = databasePath ?? throw new ArgumentNullException(nameof(databasePath));
    }

    public async Task InitializeAsync()
    {
        var path = _databasePath.FilePath;
        _databasePath.CopyLegacyDatabaseIfNeeded();

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var databaseExists = File.Exists(path);
        var version = 0;
        var malformedStepTable = false;
        var hasOrphanedChildren = false;
        if (databaseExists)
        {
            version = await ReadUserVersionAsync(path);

            // A database written by a newer application version must be left
            // untouched: no backup, no repair, no orphan cleanup and no
            // user_version rewrite (which would silently downgrade it).
            if (version > CurrentVersion)
            {
                throw new SchemaVersionTooNewException(version, CurrentVersion);
            }

            malformedStepTable = await HasMalformedStepTableAsync(path);
            hasOrphanedChildren = await HasOrphanedChildrenAsync(path);
        }

        string? backupPath = null;
        if (databaseExists &&
            (version < CurrentVersion || malformedStepTable || hasOrphanedChildren))
        {
            // Required before any operation that may alter or repair data.
            // A failure here aborts the migration via DatabaseBackupException.
            backupPath = CreateBackup(path);
        }

        var connection = new SQLiteAsyncConnection(path);
        try
        {
            // CreateTables is additive: existing tables keep their data,
            // missing columns are added, existing columns are untouched.
            await connection.CreateTableAsync<KnowledgeEntry>();
            if (malformedStepTable)
            {
                await connection.RunInTransactionAsync(RebuildStepTable);
            }
            await connection.CreateTableAsync<Step>();
            await connection.CreateTableAsync<AttachmentInfo>();

            await connection.RunInTransactionAsync(conn =>
            {
                conn.Execute(
                    "DELETE FROM \"Step\" WHERE NOT EXISTS (" +
                    "SELECT 1 FROM \"KnowledgeEntry\" " +
                    "WHERE \"KnowledgeEntry\".\"Id\" = \"Step\".\"KnowledgeEntryId\")");
                conn.Execute(
                    "DELETE FROM \"AttachmentInfo\" WHERE NOT EXISTS (" +
                    "SELECT 1 FROM \"KnowledgeEntry\" " +
                    "WHERE \"KnowledgeEntry\".\"Id\" = \"AttachmentInfo\".\"KnowledgeEntryId\")");
                conn.Execute($"PRAGMA user_version = {CurrentVersion}");
            });
        }
        catch (Exception ex)
        {
            // Stop instead of starting with a partially repaired database;
            // when a safety copy exists, point the caller at it.
            throw new SchemaMigrationException(backupPath, ex);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
```

### Pitfalls

- **`CreateTableAsync<T>` is additive.** It adds missing columns but never
  drops or renames one. A real data migration needs explicit SQL.
- **Bump `CurrentVersion` only together with the migration and its tests.**
  The version is the contract between the application and the file on disk.
- **Everything that can fail runs inside a transaction**, and the failure is
  wrapped in `SchemaMigrationException` so the caller can show a message
  instead of a stack trace.

---

## C2. Refusing to touch a database from a newer version

**What it does.** Stops the application cleanly when the database was written
by a more recent build, instead of corrupting it.

```csharp
    private static async Task<int> ReadUserVersionAsync(string path)
    {
        var connection = new SQLiteAsyncConnection(path);
        try
        {
            return await connection.ExecuteScalarAsync<int>("PRAGMA user_version");
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
```

The guard is the `version > CurrentVersion` test in C1, and the exception is
distinct so the UI can say something useful:

```csharp
public sealed class SchemaVersionTooNewException : Exception
{
    public SchemaVersionTooNewException(int databaseVersion, int supportedVersion)
        : base($"The database version {databaseVersion} is newer than the supported version {supportedVersion}.")
    {
        DatabaseVersion = databaseVersion;
        SupportedVersion = supportedVersion;
    }

    public int DatabaseVersion { get; }
    public int SupportedVersion { get; }
}
```

### Pitfalls

- **Never rewrite `user_version` when it is higher than the code supports.**
  That silently downgrades the file and the next older build will happily
  migrate it backwards.
- The check must run **before** the backup and before any repair, so a
  database from the future is not even copied around.

---

## C3. A safety backup before anything is modified

**What it does.** Keeps a consistent snapshot of the database before a
migration or a repair, and aborts if the snapshot cannot be written.

```csharp
    private static string CreateBackup(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
        var backupPath = Path.Combine(directory, $"{name}.backup.{timestamp}{extension}");

        if (File.Exists(backupPath))
        {
            throw new DatabaseBackupException(
                backupPath,
                new IOException($"A backup already exists at '{backupPath}'."));
        }

        try
        {
            using var source = new SQLiteConnection(path);
            source.Backup(backupPath, "main");
        }
        catch (Exception ex)
        {
            throw new DatabaseBackupException(backupPath, ex);
        }

        return backupPath;
    }
```

### Pitfalls

- **Use SQLite's backup API, not `File.Copy`.** A plain copy of a database
  that is open, or that has a rollback journal / WAL, can be inconsistent.
  `source.Backup(target, "main")` produces a consistent snapshot.
- **Never overwrite an existing backup.** The timestamp has millisecond
  resolution, but a collision must still fail loudly rather than destroy the
  previous copy.
- **If the backup fails, abort the whole operation.** That is the point of
  `DatabaseBackupException`: the user is told that nothing was changed.

---

## C4. Repairing a table whose primary key is malformed

**What it does.** Rebuilds a table whose primary key is not the expected one,
transactionally, without losing rows.

**Where it lives.** `Data/SchemaMigrator.cs`.

### Detecting the problem

```csharp
    private static async Task<bool> HasMalformedStepTableAsync(string path)
    {
        var connection = new SQLiteAsyncConnection(path);
        try
        {
            var exists = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Step'");
            if (exists == 0)
            {
                return false;
            }

            var columns = await connection.QueryAsync<TableColumn>("PRAGMA table_info(\"Step\")");
            var primaryKeyColumns = columns.Where(column => column.pk > 0).ToList();
            return primaryKeyColumns.Count != 1 ||
                !string.Equals(primaryKeyColumns[0].name, "Id", StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
```

### The repair

```csharp
    private static void RebuildStepTable(SQLiteConnection connection)
    {
        const string temporaryTable = "__KnowledgeTracker_Step_rebuild";
        var columns = connection.Query<TableColumn>("PRAGMA table_info(\"Step\")");
        var columnNames = columns.Select(column => column.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requiredColumns = new[] { "Description", "Order", "KnowledgeEntryId" };
        if (requiredColumns.Any(column => !columnNames.Contains(column)))
        {
            throw new InvalidDataException(
                "The existing Step table is missing columns required to safely repair its primary key.");
        }

        var tempExists = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = ?", temporaryTable);
        if (tempExists > 0)
        {
            throw new InvalidDataException(
                $"Cannot repair the Step table because '{temporaryTable}' already exists.");
        }

        var rowCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM \"Step\"");
        var hasUniqueIds = columnNames.Contains("Id") &&
            connection.ExecuteScalar<int>(
                "SELECT CASE WHEN COUNT(*) = COUNT(DISTINCT \"Id\") " +
                "AND COUNT(*) = COUNT(\"Id\") THEN 1 ELSE 0 END FROM \"Step\"") == 1;

        connection.Execute($"ALTER TABLE \"Step\" RENAME TO \"{temporaryTable}\"");
        connection.CreateTable<Step>();

        var insertColumns = hasUniqueIds
            ? "\"Id\", \"Description\", \"Order\", \"KnowledgeEntryId\""
            : "\"Description\", \"Order\", \"KnowledgeEntryId\"";
        var selectColumns = hasUniqueIds
            ? insertColumns
            : "\"Description\", \"Order\", \"KnowledgeEntryId\"";
        connection.Execute(
            $"INSERT INTO \"Step\" ({insertColumns}) " +
            $"SELECT {selectColumns} FROM \"{temporaryTable}\"");

        var rebuiltCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM \"Step\"");
        if (rebuiltCount != rowCount)
        {
            throw new InvalidDataException(
                $"Step table repair copied {rebuiltCount} of {rowCount} records.");
        }

        connection.Execute($"DROP TABLE \"{temporaryTable}\"");
    }
```

### Pitfalls

- **Check the required columns before touching anything.** If a column the
  new schema needs is missing, refuse to repair rather than losing data.
- **Check the temporary table does not exist.** A leftover from an interrupted
  repair would make the `ALTER TABLE` fail halfway.
- **Verify the row count after the copy.** A mismatch throws and the
  transaction rolls back — this is the last line of defence.
- **Preserve the ids only when they are unique.** The `hasUniqueIds` check
  decides whether the `Id` column can be copied; otherwise the new rows get
  fresh ids.
- The whole repair runs inside `RunInTransactionAsync`, so a failure leaves
  the original table untouched.

---

## C5. Cleaning up orphaned children

```csharp
    private static async Task<bool> HasOrphanedChildrenAsync(string path)
    {
        var connection = new SQLiteAsyncConnection(path);
        try
        {
            var hasEntries = await TableExistsAsync(connection, "KnowledgeEntry");
            if (await TableExistsAsync(connection, "Step"))
            {
                var orphanedSteps = hasEntries
                    ? await connection.ExecuteScalarAsync<int>(
                        "SELECT COUNT(*) FROM \"Step\" child " +
                        "WHERE NOT EXISTS (SELECT 1 FROM \"KnowledgeEntry\" parent " +
                        "WHERE parent.\"Id\" = child.\"KnowledgeEntryId\")")
                    : await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Step\"");
                if (orphanedSteps > 0)
                {
                    return true;
                }
            }
            // ... same for AttachmentInfo ...
            return false;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
```

The cleanup itself is the `DELETE … WHERE NOT EXISTS` pair in C1, executed in
the same transaction that writes the new `user_version`.

---

## C6. Where the database lives: Debug vs Release vs MSIX

**What it does.** Uses the solution folder while developing, and a writable
per-user folder when installed — which is mandatory for MSIX, because
`C:\Program Files\WindowsApps` is read-only.

**Where it lives.** `Data/DatabasePath.cs`.

```csharp
public sealed class DatabasePath
{
    internal const string DataFolderName = "KnowledgeTrackerApp";
    private const string DatabaseFileName = "knowledge.db3";

    public string FilePath { get; }
    public string? LegacyFilePath { get; }

    public static string GetDefaultPath() =>
        ResolveDefaultPath(AppContext.BaseDirectory, IsDevelopmentBuild);

    internal static string ResolveDefaultPath(
        string applicationBaseDirectory,
        bool isDevelopmentBuild,
        string? localAppDataDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationBaseDirectory);

        if (isDevelopmentBuild)
        {
            return Path.Combine(
                FindApplicationRoot(applicationBaseDirectory),
                DatabaseFileName);
        }

        var localAppData = string.IsNullOrWhiteSpace(localAppDataDirectory)
            ? GetLocalAppDataDirectory()
            : localAppDataDirectory!;
        return Path.Combine(localAppData, DataFolderName, DatabaseFileName);
    }

    private static string GetLocalAppDataDirectory() =>
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
}
```

### Finding the solution root while debugging

```csharp
    private static string FindApplicationRoot(string applicationBaseDirectory)
    {
        var directory = new DirectoryInfo(applicationBaseDirectory);
        string? projectDirectory = null;

        while (directory is not null)
        {
            if (directory.EnumerateFiles("*.sln").Any() ||
                directory.EnumerateFiles("*.slnx").Any())
            {
                return directory.FullName;
            }

            if (projectDirectory is null && directory.EnumerateFiles("*.csproj").Any())
            {
                projectDirectory = directory.FullName;
            }

            directory = directory.Parent;
        }

        return projectDirectory ?? applicationBaseDirectory;
    }
```

### Copying a legacy database, safely

```csharp
    public void CopyLegacyDatabaseIfNeeded()
    {
        if (LegacyFilePath is null ||
            !File.Exists(LegacyFilePath) ||
            File.Exists(FilePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var source = new SQLiteConnection(LegacyFilePath);
        source.Backup(FilePath, "main");
    }
```

### Pitfalls

- **The folder name under `LocalAppData` is a storage identifier.** Rename it
  only together with a migration of the existing data — the same name is
  where the language and theme preferences live too.
- **A packaged app sees a redirected `LocalApplicationData`**
  (`…\Packages\<PFN>\LocalCache\Local`). The code above works unchanged,
  because it asks the environment rather than hard-coding a path.
- **The copy must happen before the first query**, and only when the target
  does not exist — otherwise it would overwrite the user's newer data with an
  older copy.

---

## C7. Consistent multi-table writes

**What it does.** Saves or deletes an entry and its children atomically.

**Where it lives.** `Data/KnowledgeRepositorySQLite.cs`.

```csharp
        public async Task SaveAsync(KnowledgeEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var steps = entry.Steps ?? new List<Step>();
            var attachments = entry.Attachments ?? new List<AttachmentInfo>();

            await _connection.RunInTransactionAsync(conn =>
            {
                UpsertEntry(conn, entry);

                // No foreign keys / cascade in this schema (ARCHITECTURE section 6):
                // children are explicitly synchronized inside the same transaction.
                conn.Execute(
                    "DELETE FROM \"Step\" WHERE KnowledgeEntryId = ?", entry.Id);
                conn.Execute(
                    "DELETE FROM \"AttachmentInfo\" WHERE KnowledgeEntryId = ?", entry.Id);

                var order = 0;
                foreach (var step in steps)
                {
                    step.KnowledgeEntryId = entry.Id;
                    step.Order = ++order;
                    conn.Insert(step);
                }

                foreach (var attachment in attachments)
                {
                    attachment.KnowledgeEntryId = entry.Id;
                    conn.Insert(attachment);
                }
            });
        }

        public async Task DeleteAsync(int id)
        {
            await _connection.RunInTransactionAsync(conn =>
            {
                // Children first, parent last. No cascade is assumed.
                conn.Execute("DELETE FROM \"Step\" WHERE KnowledgeEntryId = ?", id);
                conn.Execute(
                    "DELETE FROM \"AttachmentInfo\" WHERE KnowledgeEntryId = ?", id);
                conn.Execute("DELETE FROM \"KnowledgeEntry\" WHERE Id = ?", id);
            });
        }
```

### Pitfalls

- **Delete the children before re-inserting them.** The schema has no
  cascade, so the old rows have to be removed explicitly or they accumulate.
- **The order of the children is persisted**, not implied by the insertion
  order: the `Order` column is rewritten on every save.
- **Never do this from a Form.** It belongs in the repository; the Form calls
  the service.

---

# D. Portable data exchange

## D1. A versioned JSON document

**What it does.** Exports the whole knowledge base to a file that can be read
by a human, imported on another machine, or processed by a script.

**Where it lives.** `Services/KnowledgeExport.cs`.

**Dependencies.** `System.Text.Json`.

### The document

```csharp
public sealed class KnowledgeExport
{
    /// <summary>
    /// Format version, independent from the application version. Readers
    /// ignore files newer than they understand instead of guessing.
    /// </summary>
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<EntryDocument> Entries { get; set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson() => JsonSerializer.Serialize(this, Options);
}
```

### Reading it back

```csharp
    public static KnowledgeExport FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new KnowledgeExportFormatException();
        }

        KnowledgeExport? document;
        try
        {
            document = JsonSerializer.Deserialize<KnowledgeExport>(json, Options);
        }
        catch (JsonException)
        {
            throw new KnowledgeExportFormatException();
        }

        if (document is null)
        {
            throw new KnowledgeExportFormatException();
        }

        if (document.FormatVersion > CurrentFormatVersion)
        {
            throw new KnowledgeExportFormatException(newerFormat: true);
        }

        document.Entries ??= new List<EntryDocument>();
        return document;
    }
```

### Pitfalls

- **The DTO layer is separate from the SQLite models on purpose.** The
  persisted models carry database concerns (primary keys, parent links); the
  export is a document meant to outlive the schema. A schema change must not
  silently change the file format.
- **Refuse a newer format instead of guessing.** `newerFormat: true` lets the
  UI say "this file comes from a newer version" rather than "invalid file".
- **`UnsafeRelaxedJsonEscaping`** keeps accented characters readable in the
  file instead of `\uXXXX`.

---

## D2. Importing without ever overwriting

The exported document contains no primary keys, so an imported entry is always
a new record:

```csharp
    /// <summary>
    /// Rebuilds a persisted aggregate from a document entry. Primary keys
    /// are intentionally not restored: an imported entry is always a new
    /// record, so importing twice can never overwrite local data by id.
    /// </summary>
    public static KnowledgeEntry ToEntry(EntryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var entry = new KnowledgeEntry
        {
            Title = document.Title ?? string.Empty,
            Description = document.Description,
            DateResolved = document.DateResolved,
            ProjectName = document.ProjectName,
            Tags = document.Tags,
            IsFavorite = document.IsFavorite,
            // ... the remaining fields
        };

        var order = 0;
        foreach (var step in document.Steps ?? new List<StepDocument>())
        {
            entry.Steps.Add(new Step
            {
                Description = step.Description ?? string.Empty,
                Order = ++order,
            });
        }

        foreach (var attachment in document.Attachments ?? new List<AttachmentDocument>())
        {
            entry.Attachments.Add(new AttachmentInfo
            {
                FileName = attachment.FileName ?? string.Empty,
                FilePath = attachment.FilePath ?? string.Empty,
                DateAdded = attachment.DateAdded,
            });
        }

        return entry;
    }
```

---

## D3. Duplicate detection by identity, not by id

**Where it lives.** `Data/EntryIdentity.cs`.

```csharp
    /// <summary>
    /// The title/project pair that identifies an entry to the user. Used to
    /// detect duplicate imports without comparing database ids, which are local
    /// and never travel in an export.
    /// </summary>
    public readonly record struct EntryIdentity(string Title, string ProjectName)
    {
        /// <summary>
        /// Normalises a raw title/project into the comparison key: trimmed, with
        /// a missing project flattened to an empty string, matched ignoring case.
        /// </summary>
        public static EntryIdentity Of(string? title, string? projectName) =>
            new(
                (title ?? string.Empty).Trim(),
                (projectName ?? string.Empty).Trim());

        public bool Matches(EntryIdentity other) =>
            string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(ProjectName, other.ProjectName, StringComparison.OrdinalIgnoreCase);
    }
```

---

## D4. One classification, shared by the prompt and the write

**Where it lives.** `Services/KnowledgeEntryService.cs`.

The trick that matters here: the duplicate count shown in the prompt and the
rows actually written come from the *same* classification, so they can never
disagree.

```csharp
    public async Task<ImportResult> ImportAsync(KnowledgeExport document, ImportMode mode)
    {
        ArgumentNullException.ThrowIfNull(document);

        var classified = await ClassifyAsync(document);

        // SkipDuplicates imports only the new entries; ImportAsCopies also
        // writes the ones that already existed.
        var toInsert = mode == ImportMode.ImportAsCopies
            ? classified.Accepted.Concat(classified.Duplicates).ToList()
            : classified.Accepted;

        await _repository.InsertBatchAsync(toInsert);

        var skipped = mode == ImportMode.SkipDuplicates
            ? classified.Duplicates.Count
            : 0;

        return new ImportResult(toInsert.Count, skipped, classified.Invalid);
    }

    /// <summary>
    /// Splits a document's entries into the ones that can be imported, the
    /// ones that already exist locally and the ones that break the entry
    /// rules. Existence is read once and then extended as valid entries are
    /// accepted, so a file that repeats the same entry does not count it
    /// twice. Shared by <see cref="ImportAsync"/> and
    /// <see cref="CountDuplicatesAsync"/> so the prompt and the write agree.
    /// </summary>
    private async Task<ClassifiedImport> ClassifyAsync(KnowledgeExport document)
    {
        var candidates = document.Entries ?? new List<EntryDocument>();
        var identities = (await _repository.GetIdentitiesAsync()).ToList();

        var accepted = new List<KnowledgeEntry>();
        var duplicates = new List<KnowledgeEntry>();
        var invalid = 0;

        foreach (var candidate in candidates)
        {
            var entry = KnowledgeExport.ToEntry(candidate);

            if (EntryValidator.Validate(entry).Count > 0)
            {
                invalid++;
                continue;
            }

            var identity = EntryIdentity.Of(entry.Title, entry.ProjectName);
            if (identities.Any(existing => existing.Matches(identity)))
            {
                duplicates.Add(entry);
                continue;
            }

            identities.Add(identity);
            accepted.Add(entry);
        }

        return new ClassifiedImport(accepted, duplicates, invalid);
    }
```

And the batch insert, which resets every id so the import always creates new
rows:

```csharp
        public async Task InsertBatchAsync(IReadOnlyList<KnowledgeEntry> entries)
        {
            ArgumentNullException.ThrowIfNull(entries);

            if (entries.Count == 0)
            {
                return;
            }

            await _connection.RunInTransactionAsync(conn =>
            {
                foreach (var entry in entries)
                {
                    // An import always creates new rows; ids from the document
                    // are not restored, so a local entry is never overwritten.
                    entry.Id = 0;
                    foreach (var step in entry.Steps ?? new List<Step>())
                    {
                        step.Id = 0;
                    }

                    foreach (var attachment in entry.Attachments ?? new List<AttachmentInfo>())
                    {
                        attachment.Id = 0;
                    }

                    UpsertEntry(conn, entry);
                    // ... children inserted as in SaveAsync
                }
            });
        }
```

### Pitfalls

- **Extend the identity list as entries are accepted**, otherwise a file that
  contains the same entry twice imports it twice.
- **Entries that break the rules are counted, not rejected.** An export from
  an older version can contain entries that no longer validate; the user gets
  a summary instead of a hard failure.

---

# E. UI and interaction

## E1. A card list with interaction zones and debounced gestures

**What it does.** A list of cards where the click on the body, the click on
the favourite icon and the right click mean different things, and a double
click never opens the entry twice.

**Where it lives.** `UI/Views/EntryCard.cs`.

### The events

```csharp
        public event Action<EntryCard>? Selected;
        public event Action<EntryCard>? OpenRequested;
        public event Action<EntryCard>? FavoriteRequested;
        public event Action<EntryCard>? DeleteRequested;
        public event Action<EntryCard>? DuplicateRequested;
```

### The gesture rules

```csharp
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            HandleGesture(e.Button, e.Clicks);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            HandleDoubleClick(e.Button);
        }

        /// <summary>
        /// Applies a mouse gesture coming from the card surface or from any of the
        /// child controls that cover it. It is kept as an explicit method so the
        /// gesture rules (select on left click, open on the second click of a
        /// double click, select on right click) can be asserted directly instead
        /// of only through simulated Windows mouse messages.
        /// </summary>
        internal void HandleGesture(MouseButtons button, int clicks)
        {
            if (button == MouseButtons.Left)
            {
                RaiseSelected();

                // A second click of a double click arrives as a mouse-down with
                // Clicks == 2 (WM_LBUTTONDBLCLK); it is the "open" gesture.
                if (clicks >= 2)
                {
                    RaiseOpenRequested();
                }
            }
            else if (button == MouseButtons.Right)
            {
                RaiseSelected();
            }
        }

        private void RaiseOpenRequested()
        {
            if ((DateTime.UtcNow - _lastOpenAt).TotalMilliseconds < OpenDebounceMs)
            {
                return;
            }

            _lastOpenAt = DateTime.UtcNow;
            OpenRequested?.Invoke(this);
        }
```

`OpenDebounceMs` is 500. The second mouse-down of a double click and the
`OnMouseDoubleClick` event describe the same gesture, so the debounce
collapses them into a single open.

### Pitfalls

- **A child control that covers the card swallows the gesture.** Route the
  child's mouse events back to the card (see the `OnMouseDown` override on
  the child in `EntryCard`), otherwise clicking on a tag does nothing.
- **Keep the gesture logic in a separate method**, not inside `OnMouseDown`.
  It is then directly testable (see G1) without simulating Windows messages.
- **Use a timestamp, not a flag, for the debounce.** A flag cannot tell a
  double click from two fast single clicks.

---

## E2. In-memory search, filter and sort

**What it does.** Searches, filters and sorts the loaded collection without
touching the database.

**Where it lives.** `UI/Views/EntryListFilter.cs`.

```csharp
    public static IReadOnlyList<KnowledgeEntry> Apply(
        IEnumerable<KnowledgeEntry> source,
        string? searchText,
        ISet<string>? selectedTags,
        bool favoritesOnly,
        EntrySortMode sortMode,
        string? projectName = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var normalizedProject = string.IsNullOrWhiteSpace(projectName)
            ? null
            : projectName.Trim();

        var tags = NormalizeTags(selectedTags);

        var filtered = source.Where(entry =>
            MatchesSearch(entry, searchText) &&
            (!favoritesOnly || entry.IsFavorite) &&
            MatchesProject(entry, normalizedProject) &&
            MatchesAllTags(entry, tags));

        return Sort(filtered, sortMode).ToList();
    }
```

### The search itself

```csharp
    public static bool MatchesSearch(KnowledgeEntry entry, string? query)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var needle = query.Trim();
        if (Contains(entry.Title, needle) ||
            Contains(entry.Description, needle) ||
            Contains(entry.ProjectName, needle) ||
            Contains(entry.Technologies, needle) ||
            Contains(entry.Tags, needle) ||
            Contains(entry.Comments, needle) ||
            Contains(entry.CodeSnippet, needle) ||
            Contains(entry.ResolutionSteps, needle))
        {
            return true;
        }

        if (entry.Steps is not null)
        {
            foreach (var step in entry.Steps)
            {
                if (Contains(step.Description, needle))
                {
                    return true;
                }
            }
        }

        if (entry.Attachments is not null)
        {
            foreach (var attachment in entry.Attachments)
            {
                if (Contains(attachment.FileName, needle))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool Contains(string? value, string needle) =>
        value is not null &&
        value.Contains(needle, StringComparison.OrdinalIgnoreCase);
```

### Pitfalls

- **Do not read attachment contents.** Only the file name is searched;
  opening every attached file would freeze the UI.
- **Keep this logic free of WinForms.** It is pure, so it is unit-testable
  (see `EntryListFilterTests`) and reusable.
- **Sorting must be stable and total.** Null dates sort last in a descending
  list; the `ThenBy`/`ThenByDescending` pair makes the order deterministic.

---

## E3. A selection that survives a reload

**What it does.** Keeps the selected row selected after the list is reloaded
from the database.

```csharp
    /// <summary>
    /// Index of the previously selected entry, falling back to the first
    /// row (or -1 when empty) so selection survives a reload by Id, not by
    /// object reference (UI_DESIGN_SPEC section 8.3).
    /// </summary>
    public static int FindSelectionIndex(IReadOnlyList<KnowledgeEntry> entries, int? id)
    {
        var index = FindIndexById(entries, id);
        if (index >= 0)
        {
            return index;
        }

        return entries.Count > 0 ? 0 : -1;
    }
```

### Pitfalls

- **Never keep a reference to the selected object.** After a reload the
  objects are new instances; the id is the only stable thing.

---

## E4. Token autocomplete in a multi-value field

**What it does.** Suggests values while the user types in a field that holds
a separated list (`tags`, `technologies`).

**Where it lives.** `UI/Views/TagSuggestionHelper.cs`.

The helper is a set of pure functions over the text box content:

```csharp
    internal static class TagSuggestionHelper
    {
        internal static string CurrentToken(string? text);
        internal static bool MatchesToken(string? token, string? candidate);
        internal static IReadOnlyList<string> UsedValues(string? text);
        internal static bool ShouldOffer(string? text, string? candidate);
        internal static char PreferredSeparator(string? text);
        internal static string AppendSeparator(string? text);
        internal static string TrimTrailingSeparators(string? text);
        internal static string ReplaceCurrentToken(string? text, string value);
    }
```

The UI keeps the caret position and replaces only the token under it:

```csharp
    var token = TagSuggestionHelper.CurrentToken(textBox.Text.Substring(0, textBox.SelectionStart));
    var candidates = TagSuggestionHelper.UsedValues(textBox.Text)
        .Where(used => !TagSuggestionHelper.MatchesToken(token, used))
        .Where(used => TagSuggestionHelper.ShouldOffer(textBox.Text, used))
        .ToList();
```

### Pitfalls

- **Respect the separator the user already typed.** Switching from `,` to `;`
  mid-list produces a broken value.
- **Do not suggest a value that is already in the list**, and match the
  candidate ignoring case.
- **Replace only the current token**, never the whole text — otherwise the
  caret jumps and the existing content is lost.

---

## E5. Image gallery

**Where it lives.** `UI/Views/ImageGalleryForm.cs`.

```csharp
    public sealed class ImageGalleryForm : SfForm
    {
        public static bool IsImage(AttachmentInfo attachment);
        // Carousel navigation, open-with-default-application, and the
        // "no images" state.
    }
```

The extension check is what decides whether an attachment appears in the
gallery at all:

```csharp
        public static bool IsImage(AttachmentInfo attachment)
        {
            var extension = Path.GetExtension(attachment.FilePath);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }
```

### Pitfalls

- **The gallery only stores the path, never the file.** An attachment that
  was moved or deleted has to produce a clear "file not found" message, not
  an exception.
- **Open the file with the default application** (`Process.Start` with
  `UseShellExecute`), and handle the failure — a missing association throws.

---

## E6. Read-only code preview

**Where it lives.** `UI/Views/CodePreviewForm.cs`.

```csharp
    public sealed class CodePreviewForm : SfForm
    {
        public CodePreviewForm(string text)
        {
            Text = AppLocalization.Translate("Code preview");
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(950, 800);
            MinimumSize = new Size(400, 300);
            BackColor = AppTheme.Background;
            MinimizeBox = false;
            MaximizeBox = false;

            var preview = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                WordWrap = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Text = text,
                Font = new Font("Consolas", 12f),
                BackColor = AppTheme.Surface,
                ForeColor = AppTheme.PrimaryText,
                BorderStyle = BorderStyle.FixedSingle,
            };

            var close = new SfButton
            {
                AutoSize = false,
                Size = new Size(100, 32),
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
            };
            AppLocalization.Bind(close, "Close");
            ButtonStyler.Apply(close, ButtonRole.Secondary);

            Controls.Add(preview);
            Controls.Add(close);

            AcceptButton = close;
            CancelButton = close;

            CarbonTheme.Apply(this);
        }
    }
```

### Pitfalls

- **The preview never modifies the source.** It is a separate read-only
  window, not a second editor.
- **Use a monospace font** (`Consolas`) for code.
- **Call `CarbonTheme.Apply(this)` at the end**, after the controls are
  created — the theme walks the tree.

---

## E7. A typed navigation shell

**What it does.** Keeps the shell (`frmMain`) free of view logic and lets
views request navigation with a typed request object.

**Where it lives.** `UI/Navigation/`.

```csharp
public sealed class NavigationServiceWinForms : INavigationService, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly INavigationHost _host;
    private readonly Stack<Control> _history = new();
    private Control? _current;

    public NavigationServiceWinForms(IServiceProvider services, INavigationHost host)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public Control? CurrentView => _current;

    public bool CanGoBack => _history.Count > 0;

    public event EventHandler? Navigated;

    public void NavigateTo<TView>(params object[] args) where TView : Control
    {
        var view = CreateView<TView>(args);

        if (_current is not null)
        {
            _history.Push(_current);
        }

        _current = view;
        _host.DisplayView(view);
        Navigated?.Invoke(this, EventArgs.Empty);
    }

    public bool GoBack()
    {
        if (_history.Count == 0)
        {
            return false;
        }

        var abandoned = _current;
        _current = _history.Pop();
        _host.DisplayView(_current);
        abandoned?.Dispose();
        Navigated?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private Control CreateView<TView>(object[]? args) where TView : Control =>
        (Control)ActivatorUtilities.CreateInstance(
            _services, typeof(TView), args ?? Array.Empty<object>());
}
```

The request records keep the call sites readable:

```csharp
public sealed record LibraryNavigationRequest;
public sealed record EntryEditorRequest(int EntryId) : INavigationRequest;
public sealed record ManageStepsRequest(int EntryId) : INavigationRequest;
```

### Pitfalls

- **Dispose the abandoned view.** Otherwise every navigation leaks a control
  and its event handlers.
- **The navigation service must not touch the database.** It owns the view
  lifecycle and nothing else.
- `ActivatorUtilities.CreateInstance` resolves the view's dependencies from
  the container and injects the extra arguments.

---

# F. Startup and packaging

## F1. Injecting a secret at build time

**What it does.** Embeds a licence key in the published binary so the
installed application starts without an environment variable on the target
machine — without ever committing the key.

**Where it lives.** `KnowledgeTrackerDesktop.csproj` and `Program.cs`.

### The build side

```xml
  <!--
    The Syncfusion license key is injected at build time from the
    SYNCFUSION_LICENSE_KEY environment variable and stored as assembly
    metadata, so published builds start without requiring an environment
    variable on the target machine. The key is never committed to the
    repository. When the variable is not set, no metadata is emitted and the
    app falls back to the environment variable at runtime.
  -->
  <ItemGroup Condition="'$(SYNCFUSION_LICENSE_KEY)' != ''">
    <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
      <_Parameter1>SyncfusionLicenseKey</_Parameter1>
      <_Parameter2>$(SYNCFUSION_LICENSE_KEY)</_Parameter2>
    </AssemblyAttribute>
  </ItemGroup>
```

### The runtime side

```csharp
        private static string? ReadSyncfusionLicenseKey()
        {
            foreach (var scope in new[]
            {
                EnvironmentVariableTarget.Process,
                EnvironmentVariableTarget.User,
                EnvironmentVariableTarget.Machine,
            })
            {
                var value = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY", scope);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        /// <summary>
        /// Reads the license key that was embedded as assembly metadata at build
        /// time from the <c>SYNCFUSION_LICENSE_KEY</c> environment variable, so
        /// published builds do not require the variable to be set on the target
        /// machine. Returns <c>null</c> when it was not injected.
        /// </summary>
        private static string? ReadEmbeddedLicenseKey()
        {
            var value = typeof(Program).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == "SyncfusionLicenseKey")
                ?.Value;

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
```

And the usage:

```csharp
            var licenseKey = ReadSyncfusionLicenseKey() ?? ReadEmbeddedLicenseKey();
            if (licenseKey is null)
            {
                MessageBox.Show(
                    AppLocalization.Translate("The Syncfusion license key was not found. ..."),
                    "KnowledgeTracker",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
```

### Pitfalls

- **The environment variable wins over the embedded key.** That is what makes
  it possible to test a different key without rebuilding.
- **The key is in the binary.** It is not a secret in the cryptographic sense;
  do not put a credential that matters in an assembly attribute. A licence
  key, a tenant id or an endpoint are fine.
- **Never commit the key.** It lives in the environment of the build machine.

---

## F2. Startup errors that say something useful

**What it does.** Turns a set of expected failure modes into distinct,
actionable messages, without ever showing a stack trace.

**Where it lives.** `Program.cs`.

```csharp
            try
            {
                schemaMigrator.InitializeAsync().GetAwaiter().GetResult();
            }
            catch (SchemaVersionTooNewException ex)
            {
                // The database belongs to a newer application version; it was left
                // untouched, so the user only needs to update the application.
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(AppLocalization.Translate(
                    "The knowledge base was created by a newer version of KnowledgeTracker. " +
                    "Update the application and try again."));
                return;
            }
            catch (DatabaseBackupException ex)
            {
                // The safety copy required before upgrading could not be written,
                // so nothing was changed.
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(AppLocalization.Translate(
                    "A safety copy of the knowledge base could not be created, so no changes were made. " +
                    "Check that there is enough free disk space and try again."));
                return;
            }
            catch (SchemaMigrationException ex)
            {
                // The upgrade or repair failed. The database is not left partially
                // repaired and any existing safety copy is reported, never restored.
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(ex.BackupPath is null
                    ? AppLocalization.Translate("The knowledge base could not be upgraded. Please try again.")
                    : string.Format(
                        AppLocalization.Culture,
                        AppLocalization.Translate(
                            "The knowledge base could not be upgraded. A safety copy was saved here:\r\n\r\n{0}"),
                        ex.BackupPath));
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ShowStartupError(AppLocalization.Translate(
                    "The knowledge base could not be opened. Please try again."));
                return;
            }
```

### Pitfalls

- **One exception type per failure mode.** It is what makes the messages
  specific instead of generic.
- **Log the full exception with `Debug.WriteLine`**, show only the message.
- **Never restore the backup automatically.** Report its path and let the
  user decide.

---

## F3. Packaging a WinForms application as MSIX

**What it does.** Produces a self-contained `win-x64` package that installs
with `Add-AppxPackage`, signed with a code-signing certificate.

**Where it lives.** `KnowledgeTrackerDesktop.Package\*.wapproj`,
`scripts/build-msix.ps1`.

### The packaging project

```xml
    <OutputType>AppContainerExe</OutputType>
    <TargetPlatformIdentifier>UAP</TargetPlatformIdentifier>
    <TargetPlatformVersion Condition="'$(TargetPlatformVersion)' == ''">10.0.19041.0</TargetPlatformVersion>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
    <AppxAutoIncrementPackageRevision>False</AppxAutoIncrementPackageRevision>
    <AppxBundle>Never</AppxBundle>
    <AppxPackageDir>$(MSBuildThisFileDirectory)..\AppPackages\</AppxPackageDir>

    <!-- The WinForms project whose .exe becomes the package entry point. -->
    <EntryPointProjectUniqueName>..\KnowledgeTrackerDesktop\KnowledgeTrackerDesktop.csproj</EntryPointProjectUniqueName>

    <!-- Package the app self-contained for x64 so no .NET runtime is required on the target machine. -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
```

### The manifest

```xml
  <Identity
    Name="KnowledgeTrackerDesktop"
    Publisher="CN=KnowledgeTrackerDev"
    Version="1.0.4.0" />
```

### The build script

```powershell
# Locate MSBuild from Visual Studio (required for .wapproj / DesktopBridge).
$vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vswhere)) {
    throw "vswhere.exe not found. Install Visual Studio with the 'Windows application packaging project' workload."
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
    Select-Object -First 1
if (-not $msbuild) {
    throw "MSBuild.exe not found via vswhere. Install Visual Studio with the 'Windows application packaging project' workload."
}

$arguments = @(
    $wapproj,
    "/p:Configuration=$Configuration",
    "/p:Platform=$Platform",
    "/p:AppxBundle=Never",
    "/p:AppxPackageSigningEnabled=true",
    "/p:PackageCertificateKeyFile=$PackageCertificateKeyFile",
    "/p:PackageCertificatePassword=$PackageCertificatePassword"
)

& $msbuild $arguments
```

### Creating a development certificate

```powershell
    $cert = New-SelfSignedCertificate `
        -Type Custom `
        -Subject $CertificateSubject `
        -KeyUsage DigitalSignature `
        -FriendlyName "KnowledgeTracker MSIX (dev)" `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")

    $securePassword = ConvertTo-SecureString -String $PackageCertificatePassword -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $defaultPfx -Password $securePassword | Out-Null
    Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
```

And trusting it once, as administrator:

```powershell
Import-Certificate -FilePath .\scripts\KnowledgeTrackerDev.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
Add-AppxPackage .\AppPackages\KnowledgeTrackerDesktop.Package_1.0.4.0_x64_Test\KnowledgeTrackerDesktop.Package_1.0.4.0_x64.msix
```

### Pitfalls

- **`dotnet build` cannot build a `.wapproj` project.** MSBuild from Visual
  Studio with the Windows application packaging workload is required.
- **`Publisher` must match the certificate subject exactly.** A mismatch
  produces a package that refuses to install.
- **Bump `Identity/@Version` for every new package.** MSIX requires an
  increasing version; re-installing the same version works only as an update.
- **The install folder is read-only.** Any file the application writes at
  runtime has to go to `LocalApplicationData` (see C6).
- **The package-private data folder starts empty.** To carry data over to a
  new installation, copy the database into
  `%LOCALAPPDATA%\Packages\<PackageFamilyName>\LocalCache\Local\<Folder>\`
  **before the first launch**.

---

## F4. A composition root

**What it does.** Wires the application together in exactly one place.

**Where it lives.** `Bootstrap/AppComposition.cs`.

```csharp
    public static class AppComposition
    {
        public static ServiceProvider Build(string? databasePath = null)
        {
            var services = new ServiceCollection();
            Register(services, databasePath);
            return services.BuildServiceProvider();
        }

        public static void Register(IServiceCollection services, string? databasePath = null)
        {
            // Persistence ------------------------------------------------
            services.AddSingleton(new DatabasePath(databasePath));
            services.AddSingleton(sp => new SQLiteAsyncConnection(
                MsDi.GetRequiredService<DatabasePath>(sp).FilePath));
            services.AddSingleton<IKnowledgeRepository, KnowledgeRepositorySQLite>();
            services.AddSingleton<SchemaMigrator>();

            // Application services --------------------------------------
            services.AddSingleton<IKnowledgeEntryService, KnowledgeEntryService>();
            services.AddSingleton<IStatisticsService, StatisticsService>();

            // WinForms shell and navigation -----------------------------
            // The shell resolves and owns the hosted views, so it is registered
            // once as the singleton navigation host.
            services.AddSingleton<frmMain>();
            services.AddSingleton<INavigationHost>(sp => MsDi.GetRequiredService<frmMain>(sp));
            services.AddSingleton<IShellHost>(sp => MsDi.GetRequiredService<frmMain>(sp));
            services.AddSingleton<INavigationService, NavigationServiceWinForms>();
        }
    }
```

### Pitfalls

- **Everything is a singleton.** A WinForms application has one database, one
  shell and one set of services. Registering a Form as transient would create
  a new one on every resolution.
- **The alias is not optional.** The WinForms Designer SDK (a transitive
  dependency of Syncfusion) brings a conflicting
  `System.ServiceExtensions.GetRequiredService` into scope:

  ```csharp
  using MsDi = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions;
  ```

---

# G. Testing WinForms without opening a window

## G1. Reflection helpers for gestures and control-tree queries

**What it does.** Asserts the behaviour of a WinForms control in a normal
xUnit test — no designer, no message loop, no window.

**Where it lives.** `KnowledgeTrackerDesktop.Tests\UI\*.cs` (30 files).

### Finding a control by its content

```csharp
        private static IEnumerable<Control> FindControls(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var descendant in FindControls(child))
                {
                    yield return descendant;
                }
            }
        }
```

Used to assert on what a card actually shows:

```csharp
        [Fact]
        public void CardShowsFieldsWithoutClippingRegionOrDescription()
        {
            using var card = new EntryCard(new KnowledgeEntry
            {
                Id = 11,
                Title = "Visible card",
                Description = "Hidden description",
                ProjectName = "Internal tool",
                Tags = "ui, test",
            });

            var texts = FindControls(card).OfType<Label>().Select(label => label.Text).ToArray();

            Assert.Null(card.Region);
            Assert.Contains("Visible card", texts);
            Assert.DoesNotContain(texts, text => text == "Hidden description");
            Assert.Contains("#ui", texts);
            Assert.Contains("#test", texts);
        }
```

### Simulating mouse gestures

```csharp
        private static void InvokeMouseDown(Control control, MouseButtons button, int clicks = 1)
        {
            var method = typeof(Control).GetMethod(
                "OnMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { new MouseEventArgs(button, clicks, 2, 2, 0) });
        }

        private static void InvokeMouseDoubleClick(Control control, MouseButtons button)
        {
            var method = typeof(Control).GetMethod(
                "OnMouseDoubleClick",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { new MouseEventArgs(button, 2, 2, 2, 0) });
        }

        private static void InvokeOnClick(Control control)
        {
            var method = typeof(Control).GetMethod(
                "OnClick",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            method.Invoke(control, new object[] { EventArgs.Empty });
        }
```

### Asserting that a zone does not trigger the wrong action

```csharp
        [Fact]
        public void DeleteAffordanceDoesNotSelectOrOpenAndRaisesDelete()
        {
            using var card = new EntryCard(new KnowledgeEntry { Id = 4, Title = "Delete me" });
            var selected = false;
            var opened = false;
            var deleted = false;
            card.Selected += _ => selected = true;
            card.OpenRequested += _ => opened = true;
            card.DeleteRequested += _ => deleted = true;
            var delete = FindControls(card)
                .OfType<Label>()
                .Single(control => control.AccessibleName == "Delete");

            InvokeMouseDown(delete, MouseButtons.Left);
            InvokeOnClick(delete);

            Assert.False(selected);
            Assert.False(opened);
            Assert.True(deleted);
        }
```

### Pitfalls

- **`AccessibleName` is the stable handle.** Icon buttons drawn from a font
  glyph have no text; give them an accessible name and find them by it in the
  tests.
- **Call the gesture method directly when it exists.** `card.HandleGesture(
  MouseButtons.Left, 2)` is more precise than simulating a message, and it
  makes the rules readable as documentation.
- **`using` the control** disposes it at the end of the test; without it the
  handles accumulate across a large suite.

---

## G2. An isolated database per test

**Where it lives.** `KnowledgeTrackerDesktop.Tests\Data\TempDatabase.cs`.

```csharp
    internal sealed class TempDatabase : IDisposable
    {
        public string DirectoryPath { get; }

        public string DbPath => Path.Combine(DirectoryPath, "knowledge.db3");

        public TempDatabase()
        {
            DirectoryPath = Path.Combine(
                Path.GetTempPath(),
                "knowledgetracker-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(DirectoryPath);
        }

        public DatabasePath DatabasePath => new(DbPath);

        public SQLiteAsyncConnection OpenConnection() => new(DbPath);

        public void Dispose()
        {
            try
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
            catch (IOException)
            {
                // The sqlite-net connection pool can still hold the file
                // briefly; leftovers in %TEMP% are harmless.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
```

### Pitfalls

- **Never point a test at the user's database.** The `DatabasePath` class
  accepts an explicit path precisely so a test can override the default.
- **The connection pool keeps the file open for a moment** after the test, so
  the delete can fail; swallow it and leave the file in `%TEMP%`.

---

# H. Extraction checklist for a new application

Ordered by value per unit of effort.

| # | Take this | Effort | Why |
|---|---|---|---|
| 1 | A4 — design tokens | minutes | Nothing else can be styled without it. |
| 2 | A1 — the Syncfusion dark theme | half a day | The hardest part of the visual identity; the pitfalls are already solved. |
| 3 | A2 — semantic button roles | 1 hour | Keeps the views free of colours. |
| 4 | A3 — rounded corners | 1 hour | Cards and panels stop looking like a form. |
| 5 | C3 + C1 + C2 — backups, migrations, version guard | 1 day | The part nobody wants to write twice, and where a mistake loses data. |
| 6 | C6 — where the database lives | 1 hour | Mandatory for MSIX. |
| 7 | G1 + G2 — testing WinForms without a UI | half a day | Makes every other change safe to make. |
| 8 | B1 — runtime localization | half a day | Add the strings as you write the views, not afterwards. |
| 9 | F1 — secret at build time | 1 hour | Small, and needed by any licensed control. |
| 10 | F3 — MSIX packaging | half a day | Copy the `.wapproj`, the manifest and the script. |
| 11 | D1–D4 — portable JSON | 1 day | Gives interoperability and a path to synchronisation later. |
| 12 | E7 — typed navigation | 2 hours | Keeps the shell free of view logic. |
| 13 | A5, E1–E6 — the UI pieces | 1–2 days | Adapt to the new domain; the gestures and the interaction zones transfer directly. |

## What does not transfer

- `ctlAddEdit` (the four-tab editor) and `ctlMain` (the library view). The
  *pattern* transfers — a card list, a preview pane, an editor split into
  General / Attachments / Link / Resolutions — but the code is 66 KB and 43 KB
  of domain-specific layout.
- The translation dictionaries. The mechanism transfers; the strings do not.
- The `KnowledgeEntry` / `Step` / `AttachmentInfo` schema, unless the new
  application has the same shape. `SchemaMigrator` is bound to those table
  names and has to be parameterised first.

## Known limitations of these patterns

- **The whole UI layer is WinForms and Syncfusion specific.** A
  cross-platform application (MAUI, Avalonia, Electron) can only reuse the
  persistence, the document format and the testing ideas — sections C, D and
  the philosophy of G.
- **The localization keys are the English source strings.** That is compact and
  easy to read in the views, but a typo is invisible. For a larger app, load
  the dictionaries from JSON or `.resx` and report the misses.
- **The theme relies on Syncfusion's theming engine.** Legacy Syncfusion
  controls (`TextBoxExt`, `ComboBoxAdv`, `CheckBoxAdv`) only accept colours
  set in code; there is no XML route for them.
