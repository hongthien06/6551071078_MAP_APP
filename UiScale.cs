namespace HotelManagement
{
    internal static class UiScale
    {
        public static float Factor { get; private set; } = 1f;

        public static void FitToScreen(Form form)
        {
            Rectangle wa = Screen.FromControl(form).WorkingArea;
            float f = Math.Min(1f, Math.Min((float)wa.Width / form.Width, (float)wa.Height / form.Height));
            if (f >= 0.999f)
                return;
            Factor = f;
            Apply(form, f);
            form.Location = new Point(wa.Left + (wa.Width - form.Width) / 2, wa.Top + (wa.Height - form.Height) / 2);
        }

        public static void ApplyToChild(Control child)
        {
            if (Factor < 0.999f && child.Tag as string != "uiscaled")
            {
                Apply(child, Factor);
                child.Tag = "uiscaled";
            }
        }

        public static void Apply(Control root, float f)
        {
            ScaleFonts(root, f);
            root.Scale(new SizeF(f, f));
        }

        private static void ScaleFonts(Control root, float f)
        {
            var fonts = new List<(Control, Font)>();
            void Collect(Control c)
            {
                fonts.Add((c, c.Font));
                foreach (Control child in c.Controls)
                    Collect(child);
            }
            Collect(root);
            foreach (var (c, font) in fonts)
                c.Font = new Font(font.FontFamily, font.Size * f, font.Style, font.Unit);
        }
    }
}
