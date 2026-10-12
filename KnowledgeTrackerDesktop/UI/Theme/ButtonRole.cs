namespace KnowledgeTrackerDesktop.UI.Theme
{
    /// <summary>
    /// Functional weight of a button, used to derive a coherent set of colours
    /// for its normal, hover, pressed, focused and disabled states. Keeps the
    /// visual hierarchy explicit instead of giving every button one static
    /// colour (UI_DESIGN_SPEC: modern dark-theme button hierarchy).
    /// </summary>
    internal enum ButtonRole
    {
        /// <summary>Primary action / call to action (Save, Submit, Login).</summary>
        Primary,

        /// <summary>Secondary action (Cancel, Back) with less visual weight.</summary>
        Secondary,

        /// <summary>Tertiary / ghost action (Learn more, Close): text only.</summary>
        Ghost,

        /// <summary>Destructive action (Delete, Remove) in a muted red.</summary>
        Danger,

        /// <summary>Navigation rail item in its resting (unselected) state.</summary>
        Nav,

        /// <summary>Navigation rail item for the currently displayed primary view.</summary>
        NavActive,

        /// <summary>Transparent icon-only action (arrows, chevrons) with no chrome.</summary>
        Icon,

        /// <summary>Transparent icon-only destructive action (remove "X").</summary>
        IconDanger,
    }
}
