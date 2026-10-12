using KnowledgeTrackerDesktop.UI;
using KnowledgeTrackerDesktop.UI.Views;
using System.Runtime.CompilerServices;
using Syncfusion.WinForms.Controls;
using Syncfusion.WinForms.Input;
using Syncfusion.Windows.Forms.Tools;

namespace KnowledgeTrackerDesktop.UI.Theme
{
    /// <summary>
    /// Applies the CarbonDark palette through Syncfusion's skinning system.
    ///
    /// The strategy (verified against Syncfusion 35.1.39):
    ///  1. A built-in dark visual style (<see cref="CarbonPalette.BaseVisualStyle"/>)
    ///     is applied to the whole tree. Legacy controls only accept
    ///     <c>VisualTheme</c> enum names, so the custom theme name must not be
    ///     set on them or on a container that owns them.
    ///  2. Modern Sf controls that accept arbitrary theme names are switched to
    ///     the embedded <see cref="CarbonPalette.ThemeName"/> theme (see the
    ///     CarbonDark XML resources).
    ///  3. Legacy controls (TextBoxExt, ComboBoxAdv, CheckBoxAdv, ...) cannot
    ///     load a custom theme name, so their colours are set explicitly in code.
    ///
    /// Category mapping:
    ///  - Windows / layout (SfForm, Panel, splitter): base dark + DeepBackground,
    ///    rounded corners on SfForm.
    ///  - Input (TextBoxExt, ComboBoxAdv, CheckBoxAdv, SfDateTimeEdit): custom
    ///    theme XML / explicit overrides with the palette.
    ///  - Actions (SfButton): custom theme XML (accent-filled primary buttons).
    ///  - Data display / navigation (SfDataGrid, SfListView, TreeViewAdv, tabs,
    ///    menus): inherit the base dark style; a CarbonDark XML can be dropped
    ///    under Themes\CarbonDark to customise each control as they are added.
    /// </summary>
    internal static class CarbonTheme
    {
        private static readonly object Gate = new();
        private static readonly ConditionalWeakTable<ComboBoxAdv, object> ComboBoxHandleHooks = new();
        private static bool _initialized;

        /// <summary>
        /// Registers the theme assemblies once. Call before applying the theme.
        /// </summary>
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

        /// <summary>
        /// Applies the CarbonDark skin to <paramref name="root"/> and every
        /// descendant control. Safe to call on the shell and again on each view
        /// as it is shown.
        /// </summary>
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

            if (root is ctlAddEdit editor)
            {
                editor.ApplyEditorTabTheme();
            }
        }

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
                    break;
                case SfNumericTextBox numericTextBox:
                    numericTextBox.ThemeName = CarbonPalette.ThemeName;
                    break;
                case TextBoxExt textBox:
                    ApplyTextBoxExt(textBox);
                    break;
                case ComboBoxAdv comboBox:
                    ApplyComboBoxAdv(comboBox);
                    break;
                case ComboBoxFrame comboBoxFrame:
                    comboBoxFrame.ApplyCarbonStyle();
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

        private static void ApplyTextBoxExt(TextBoxExt textBox)
        {
            textBox.BackColor = CarbonPalette.DeepBackground;
            textBox.ForeColor = CarbonPalette.TextPrimary;
            textBox.BorderColor = CarbonPalette.Border;
            textBox.FocusBorderColor = CarbonPalette.Accent;
            textBox.Font = AppTheme.Fonts.Input;
        }

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

        private static void ApplyCheckBoxAdv(CheckBoxAdv checkBox)
        {
            checkBox.BackColor = CarbonPalette.Surface;
            checkBox.ForeColor = CarbonPalette.TextPrimary;
        }
    }
}