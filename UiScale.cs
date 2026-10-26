namespace HotelManagement
{
    // Form thiết kế cho màn hình lớn (client ~1745x1055). Nếu lớn hơn vùng làm việc của màn hình
    // hiện tại thì thu nhỏ đồng đều cả form chính lẫn các form con (giữ nguyên bố cục).
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

        // Dùng cho form/control con được nhúng vào panelMain sau khi form chính đã thu nhỏ
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
            // Đọc font gốc của tất cả control trước, rồi mới gán, để font kế thừa không bị nhân đôi
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
