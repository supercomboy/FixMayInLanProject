using FixMayInLan.Models;

namespace FixMayInLan.UI;

public static class ThemeManager
{
    private static readonly Color Navy =
        Color.FromArgb(20, 31, 49);

    private static readonly Color Blue =
        Color.FromArgb(35, 99, 235);

    private static readonly Color Danger =
        Color.FromArgb(190, 40, 40);

    private static readonly Color LightCanvas =
        Color.FromArgb(242, 245, 250);

    private static readonly Color LightSurface =
        Color.White;

    private static readonly Color LightSurfaceAlt =
        Color.FromArgb(237, 242, 250);

    private static readonly Color LightText =
        Color.FromArgb(20, 31, 49);

    private static readonly Color LightSecondary =
        Color.FromArgb(100, 116, 139);

    private static readonly Color LightBorder =
        Color.FromArgb(203, 213, 225);

    private static readonly Color DarkCanvas =
        Color.FromArgb(13, 20, 31);

    private static readonly Color DarkSurface =
        Color.FromArgb(24, 34, 49);

    private static readonly Color DarkSurfaceAlt =
        Color.FromArgb(34, 47, 65);

    private static readonly Color DarkText =
        Color.FromArgb(233, 239, 247);

    private static readonly Color DarkSecondary =
        Color.FromArgb(158, 171, 190);

    private static readonly Color DarkBorder =
        Color.FromArgb(65, 81, 105);

    public static AppTheme CurrentTheme
    {
        get;
        private set;
    } = AppTheme.Light;

    public static void Apply(
        Control root,
        AppTheme theme)
    {
        CurrentTheme = theme;

        ThemePalette palette =
            theme == AppTheme.Dark
                ? CreateDarkPalette()
                : CreateLightPalette();

        ApplyControl(
            root,
            palette);
    }

    private static void ApplyControl(
        Control control,
        ThemePalette palette)
    {
        switch (control)
        {
            case Form form:
                form.BackColor = palette.Canvas;
                form.ForeColor = palette.Text;
                break;

            case DataGridView grid:
                ApplyDataGridTheme(
                    grid,
                    palette);
                break;

            case ListView listView:
                listView.BackColor =
                    palette.Surface;

                listView.ForeColor =
                    palette.Text;
                break;

            case RichTextBox richTextBox:
                // Giữ nguyên console log màu Navy.
                if (richTextBox.BackColor != Navy)
                {
                    richTextBox.BackColor =
                        palette.Surface;

                    richTextBox.ForeColor =
                        palette.Text;
                }

                break;

            case TextBox textBox:
                textBox.BackColor =
                    palette.Surface;

                textBox.ForeColor =
                    palette.Text;
                break;

            case Button button:
                ApplyButtonTheme(
                    button,
                    palette);
                break;

            case Label label:
                ApplyLabelTheme(
                    label,
                    palette);
                break;

            // TableLayoutPanel và FlowLayoutPanel đều
// kế thừa từ Panel nên chỉ cần một case.
case Panel panel:
    ApplyContainerTheme(
        panel,
        palette);
    break;
        }

        foreach (Control child in
                 control.Controls)
        {
            ApplyControl(
                child,
                palette);
        }

        control.Invalidate();
    }

    private static void ApplyContainerTheme(
        Control control,
        ThemePalette palette)
    {
        if (control.BackColor == Navy)
        {
            // Header và console log.
            return;
        }

        if (control.BackColor ==
            Color.FromArgb(255, 247, 226))
        {
            // Banner cảnh báo.
            control.BackColor =
                palette.WarningBackground;

            control.ForeColor =
                palette.WarningText;

            return;
        }

        if (control.BackColor ==
                LightSurface ||
            control.BackColor ==
                DarkSurface)
        {
            control.BackColor =
                palette.Surface;

            return;
        }

        if (control.BackColor ==
                LightSurfaceAlt ||
            control.BackColor ==
                DarkSurfaceAlt)
        {
            control.BackColor =
                palette.SurfaceAlternative;

            return;
        }

        control.BackColor =
            palette.Canvas;
    }

    private static void ApplyLabelTheme(
        Label label,
        ThemePalette palette)
    {
        // Label nằm trên Header Navy:
        // giữ màu trắng hoặc xanh nhạt.
        if (label.Parent?.BackColor == Navy)
        {
            return;
        }

        // Badge Server, Domain và Workgroup:
        // giữ màu nền riêng.
        if (label.BackColor ==
                Color.FromArgb(190, 40, 40) ||
            label.BackColor ==
                Color.FromArgb(173, 103, 0) ||
            label.BackColor ==
                Color.FromArgb(24, 128, 82) ||
            label.BackColor ==
                Color.FromArgb(100, 116, 139))
        {
            return;
        }

        if (label.BackColor ==
            Color.FromArgb(255, 247, 226))
        {
            label.BackColor =
                palette.WarningBackground;

            label.ForeColor =
                palette.WarningText;

            return;
        }

        bool usesSecondaryColor =
            label.ForeColor ==
                LightSecondary ||
            label.ForeColor ==
                DarkSecondary ||
            label.ForeColor ==
                Color.FromArgb(71, 85, 105);

        label.ForeColor =
            usesSecondaryColor
                ? palette.SecondaryText
                : palette.Text;
    }

    private static void ApplyButtonTheme(
        Button button,
        ThemePalette palette)
    {
        // Giữ màu nút chính và nút nguy hiểm.
        if (button.BackColor == Blue ||
            button.BackColor == Danger)
        {
            return;
        }

        // Nút nằm trên Header.
        if (button.Parent?.BackColor == Navy)
        {
            return;
        }

        button.BackColor =
            palette.SurfaceAlternative;

        button.ForeColor =
            palette.Text;

        button.FlatAppearance.BorderColor =
            palette.Border;
    }

    private static void ApplyDataGridTheme(
        DataGridView grid,
        ThemePalette palette)
    {
        grid.BackgroundColor =
            palette.Surface;

        grid.GridColor =
            palette.Border;

        grid.DefaultCellStyle.BackColor =
            palette.Surface;

        grid.DefaultCellStyle.ForeColor =
            palette.Text;

        grid.DefaultCellStyle.SelectionBackColor =
            Blue;

        grid.DefaultCellStyle.SelectionForeColor =
            Color.White;

        grid.AlternatingRowsDefaultCellStyle
            .BackColor =
                palette.SurfaceAlternative;

        grid.AlternatingRowsDefaultCellStyle
            .ForeColor =
                palette.Text;

        grid.ColumnHeadersDefaultCellStyle
            .BackColor =
                palette.SurfaceAlternative;

        grid.ColumnHeadersDefaultCellStyle
            .ForeColor =
                palette.Text;

        grid.EnableHeadersVisualStyles = false;
    }

    private static ThemePalette
        CreateLightPalette()
    {
        return new ThemePalette
        {
            Canvas = LightCanvas,
            Surface = LightSurface,
            SurfaceAlternative =
                LightSurfaceAlt,
            Text = LightText,
            SecondaryText =
                LightSecondary,
            Border = LightBorder,
            WarningBackground =
                Color.FromArgb(255, 247, 226),
            WarningText =
                Color.FromArgb(124, 76, 0)
        };
    }

    private static ThemePalette
        CreateDarkPalette()
    {
        return new ThemePalette
        {
            Canvas = DarkCanvas,
            Surface = DarkSurface,
            SurfaceAlternative =
                DarkSurfaceAlt,
            Text = DarkText,
            SecondaryText =
                DarkSecondary,
            Border = DarkBorder,
            WarningBackground =
                Color.FromArgb(73, 55, 24),
            WarningText =
                Color.FromArgb(255, 213, 128)
        };
    }

    private sealed class ThemePalette
    {
        public Color Canvas { get; init; }

        public Color Surface { get; init; }

        public Color SurfaceAlternative
        {
            get;
            init;
        }

        public Color Text { get; init; }

        public Color SecondaryText { get; init; }

        public Color Border { get; init; }

        public Color WarningBackground
        {
            get;
            init;
        }

        public Color WarningText { get; init; }
    }
}