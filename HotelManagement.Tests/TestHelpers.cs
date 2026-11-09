using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace HotelManagement.Tests;

/// <summary>Chạy code WinForms trên một luồng STA riêng.</summary>
internal static class Sta
{
    public static void Run(Action action)
    {
        Exception? loi = null;
        var t = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { loi = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        if (!t.Join(TimeSpan.FromSeconds(60)))
            throw new TimeoutException("Test WinForms bị treo quá 60 giây.");
        if (loi != null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(loi).Throw();
    }
}

/// <summary>Truy cập field / method private của form bằng reflection.</summary>
internal static class Rf
{
    private const BindingFlags All = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    public static T Get<T>(object obj, string name)
    {
        for (Type? t = obj.GetType(); t != null; t = t.BaseType)
        {
            var f = t.GetField(name, All | BindingFlags.DeclaredOnly);
            if (f != null) return (T)f.GetValue(obj)!;
        }
        throw new MissingFieldException(obj.GetType().Name, name);
    }

    public static object? Call(object obj, string name, params object?[] args)
    {
        var m = obj.GetType().GetMethod(name, All)
                ?? throw new MissingMethodException(obj.GetType().Name, name);
        try { return m.Invoke(m.IsStatic ? null : obj, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    public static object? CallStatic(Type type, string name, params object?[] args)
    {
        var m = type.GetMethod(name, All) ?? throw new MissingMethodException(type.Name, name);
        try { return m.Invoke(null, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }

    /// <summary>Đếm số handler đã đăng ký cho một sự kiện (Load, Click, CellClick...).</summary>
    public static int CountHandlers(Component c, string eventName)
    {
        var events = (EventHandlerList)typeof(Component)
            .GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(c)!;

        string target = eventName.ToLowerInvariant();
        for (Type? t = c.GetType(); t != null; t = t.BaseType)
        {
            foreach (var f in t.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (f.FieldType != typeof(object)) continue;
                string n = f.Name.ToLowerInvariant().Replace("s_", "").Replace("event", "").Replace("_", "");
                if (t == typeof(DataGridView)) n = n.Replace("datagridview", "");
                if (n != target) continue;
                var key = f.GetValue(null);
                return key == null ? 0 : events[key]?.GetInvocationList().Length ?? 0;
            }
        }
        throw new InvalidOperationException($"Không tìm thấy khoá sự kiện {eventName} trên {c.GetType().Name}");
    }

    public static IEnumerable<Control> AllControls(Control root)
    {
        foreach (Control c in root.Controls)
        {
            yield return c;
            foreach (var con in AllControls(c)) yield return con;
        }
    }
}

/// <summary>
/// Tự đóng MessageBox xuất hiện trên luồng hiện tại và ghi lại nội dung,
/// để test được các nhánh hiện thông báo mà không bị treo.
/// </summary>
internal sealed class MessageBoxWatcher : IDisposable
{
    private delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, EnumProc cb, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumProc cb, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
    [DllImport("user32.dll")] private static extern int GetDlgCtrlID(IntPtr h);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    private const uint WM_COMMAND = 0x0111;
    private const int IDOK = 1, IDCANCEL = 2, IDYES = 6, IDNO = 7;

    private readonly uint _threadId;
    private readonly System.Threading.Timer _timer;
    private readonly HashSet<IntPtr> _daXuLy = new();
    private readonly object _khoa = new();
    private readonly bool _chonYes;

    public List<string> Messages { get; } = new();

    public MessageBoxWatcher(bool chonYes = false)
    {
        _chonYes = chonYes;
        _threadId = GetCurrentThreadId();
        _timer = new System.Threading.Timer(_ => Quet(), null, 50, 50);
    }

    private void Quet()
    {
        EnumThreadWindows(_threadId, (h, _) =>
        {
            if (!IsWindowVisible(h)) return true;
            var cls = new StringBuilder(64);
            GetClassName(h, cls, 64);
            if (cls.ToString() != "#32770") return true;
            lock (_khoa)
            {
                if (!_daXuLy.Add(h)) return true;
            }

            var tieuDe = new StringBuilder(256);
            GetWindowText(h, tieuDe, 256);

            // Hộp thoại Lưu file: điền tên file rồi bấm Save
            if (TenFileLuu != null)
            {
                IntPtr oTen = TimOTenFile(h);
                if (oTen != IntPtr.Zero)
                {
                    SendMessage(oTen, WM_SETTEXT, IntPtr.Zero, TenFileLuu);
                    lock (_khoa) Messages.Add($"[{tieuDe}] (lưu file) {TenFileLuu}");
                    PostMessage(h, WM_COMMAND, (IntPtr)IDOK, IntPtr.Zero);
                    return true;
                }
                if (tieuDe.ToString().StartsWith("Lưu") || tieuDe.ToString().StartsWith("Xuất"))
                {
                    lock (_khoa) _daXuLy.Remove(h); // ô tên file chưa sẵn sàng, quét lại lần sau
                    return true;
                }
            }

            string noiDung = "";
            var cacNut = new List<int>();
            EnumChildWindows(h, (c, _) =>
            {
                var k = new StringBuilder(64);
                GetClassName(c, k, 64);
                if (k.ToString() == "Static")
                {
                    var tx = new StringBuilder(1024);
                    GetWindowText(c, tx, 1024);
                    if (tx.Length > 0) noiDung = tx.ToString();
                }
                else if (k.ToString() == "Button")
                {
                    cacNut.Add(GetDlgCtrlID(c));
                }
                return true;
            }, IntPtr.Zero);

            lock (_khoa) Messages.Add($"[{tieuDe}] {noiDung}");

            int nut = _chonYes && cacNut.Contains(IDYES) ? IDYES
                    : cacNut.Contains(IDNO) ? IDNO
                    : cacNut.Contains(IDOK) ? IDOK
                    : IDCANCEL;
            PostMessage(h, WM_COMMAND, (IntPtr)nut, IntPtr.Zero);
            return true;
        }, IntPtr.Zero);
    }

    /// <summary>Đường dẫn sẽ điền vào hộp thoại SaveFileDialog (null = không xử lý hộp thoại file).</summary>
    public string? TenFileLuu { get; set; }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessage(IntPtr h, uint msg, IntPtr w, string l);
    private const uint WM_SETTEXT = 0x000C;
    private const int ID_O_TEN_FILE = 0x47C; // cmb13 / edt1 của hộp thoại file kiểu cũ
    private const int ID_O_TEN_FILE_MOI = 1001;

    private static IntPtr TimOTenFile(IntPtr dialog)
    {
        IntPtr khung = IntPtr.Zero, oEdit = IntPtr.Zero;
        EnumChildWindows(dialog, (c, _) =>
        {
            var lop = new StringBuilder(64);
            GetClassName(c, lop, 64);
            // Hộp thoại kiểu mới (Vista+): ô tên file là Edit id 1001
            if (lop.ToString() == "Edit" && GetDlgCtrlID(c) == ID_O_TEN_FILE_MOI && IsWindowVisible(c)) { oEdit = c; return false; }
            if (GetDlgCtrlID(c) == ID_O_TEN_FILE) { khung = c; return false; }
            return true;
        }, IntPtr.Zero);
        if (oEdit != IntPtr.Zero) return oEdit;
        if (khung == IntPtr.Zero) return IntPtr.Zero;

        var k = new StringBuilder(64);
        GetClassName(khung, k, 64);
        if (k.ToString() == "Edit") return khung;
        EnumChildWindows(khung, (c, _) =>
        {
            var kk = new StringBuilder(64);
            GetClassName(c, kk, 64);
            if (kk.ToString() == "Edit") { oEdit = c; return false; }
            return true;
        }, IntPtr.Zero);
        return oEdit;
    }

    public void Dispose() => _timer.Dispose();
}

/// <summary>Kiểm tra CSDL LocalDB của ứng dụng có sẵn hay không (test tích hợp sẽ bỏ qua nếu không có).</summary>
internal static class Db
{
    private static bool? _coSan;

    public static bool CoSan
    {
        get
        {
            if (_coSan.HasValue) return _coSan.Value;
            try
            {
                using var db = new HotelManagementContext();
                _coSan = db.Database.CanConnect() && db.PhieuThues.Any();
            }
            catch
            {
                _coSan = false;
            }
            return _coSan.Value;
        }
    }

    public static HotelManagementContext Mo() => new HotelManagementContext();
}

/// <summary>Fact chỉ chạy khi có CSDL HotelManagement trên LocalDB.</summary>
public sealed class DbFactAttribute : Xunit.FactAttribute
{
    public DbFactAttribute()
    {
        if (!Db.CoSan)
            Skip = "Không kết nối được CSDL HotelManagement trên (localdb)\\MSSQLLocalDB";
    }
}
