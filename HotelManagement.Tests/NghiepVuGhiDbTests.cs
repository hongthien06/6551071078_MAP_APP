using ClosedXML.Excel;
using HotelManagement.DangNhap;
using HotelManagement.HoaDon;
using HotelManagement.Models;
using HotelManagement.PhieuThue;
using HotelManagement.TaiKhoan;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelManagement.Tests;

/// <summary>
/// Dữ liệu tạm cho test có ghi CSDL: phiếu PTZ99, chi tiết CTZ99xx, tài khoản tz_lab3*.
/// Dispose() xoá toàn bộ và khôi phục trạng thái phòng, số lượng dịch vụ, phiên đăng nhập.
/// </summary>
internal sealed class DuLieuTam : IDisposable
{
    public const string MaPT = "PTZ99";
    public string MaNV { get; }
    public List<string> Phong { get; }

    private readonly Dictionary<string, string> _ttPhongGoc;
    private readonly Dictionary<string, int?> _slDichVuGoc;
    private readonly (string, string, int) _phienGoc;

    public DuLieuTam()
    {
        _phienGoc = (FormDangNhap.MaNV_DangNhap, FormDangNhap.TenNV_DangNhap, FormDangNhap.CapDoQuyen_DangNhap);
        DonDep(); // phòng khi lần chạy trước bị ngắt giữa chừng

        using var db = Db.Mo();
        var dangDung = db.Ctdps.Where(c => c.DaXoa != true && (c.TrangThai == "Đã đặt" || c.TrangThai == "Đang thuê"))
            .Select(c => c.MaPh).ToList();
        Phong = db.Phongs.Where(p => p.DaXoa != true && p.Ttph == "Bình thường")
            .Select(p => p.MaPh).ToList()
            .Where(p => !dangDung.Contains(p)).OrderBy(p => p).Take(2).ToList();
        if (Phong.Count < 2) throw new InvalidOperationException("Cần ít nhất 2 phòng trống để test");

        _ttPhongGoc = db.Phongs.Where(p => Phong.Contains(p.MaPh)).ToDictionary(p => p.MaPh, p => p.Ttph);
        _slDichVuGoc = db.DichVus.ToDictionary(d => d.MaDv, d => d.SlconLai);

        MaNV = db.NhanViens.Where(n => n.DaXoa != true).OrderBy(n => n.MaNv).Select(n => n.MaNv).First();
        string maKH = db.KhachHangs.OrderBy(k => k.MaKh).Select(k => k.MaKh).First();
        db.PhieuThues.Add(new Models.PhieuThue { MaPt = MaPT, NgPt = DateTime.Today, MaKh = maKH, MaNv = MaNV, DaXoa = false });
        db.SaveChanges();

        FormDangNhap.MaNV_DangNhap = MaNV;
    }

    /// <summary>Tạo chi tiết đặt phòng; phòng chuyển trạng thái giống chức năng đặt phòng / nhận phòng.</summary>
    public Ctdp TaoCtdp(string maCtdp, string maPh, string trangThai, int soNgay = 2, int lechNgay = 0)
    {
        using var db = Db.Mo();
        var ct = new Ctdp
        {
            MaCtdp = maCtdp, MaPt = MaPT, MaPh = maPh, SoNguoi = 1,
            CheckIn = DateTime.Today.AddDays(lechNgay), CheckOut = DateTime.Today.AddDays(lechNgay + soNgay),
            TrangThai = trangThai, DaXoa = false
        };
        db.Ctdps.Add(ct);
        var phong = db.Phongs.First(p => p.MaPh == maPh);
        if (phong.Ttph == "Bình thường" || trangThai == "Đang thuê") phong.Ttph = trangThai;
        db.SaveChanges();
        return db.Ctdps.AsNoTracking().First(c => c.MaCtdp == maCtdp);
    }

    private static void DonDep()
    {
        using var db = Db.Mo();
        db.Database.ExecuteSqlRaw("DELETE FROM HoaDon WHERE MaCTDP LIKE 'CTZ99%'");
        db.Database.ExecuteSqlRaw("DELETE FROM CTDV WHERE MaCTDP LIKE 'CTZ99%'");
        db.Database.ExecuteSqlRaw("DELETE FROM CTDP WHERE MaCTDP LIKE 'CTZ99%'");
        db.Database.ExecuteSqlRaw("DELETE FROM PhieuThue WHERE MaPT = 'PTZ99'");
        db.Database.ExecuteSqlRaw("DELETE FROM TaiKhoan WHERE TenTK LIKE 'tz[_]lab3%'");
    }

    public void Dispose()
    {
        DonDep();
        using var db = Db.Mo();
        foreach (var p in db.Phongs.Where(p => Phong.Contains(p.MaPh)))
            p.Ttph = _ttPhongGoc[p.MaPh];
        foreach (var d in db.DichVus)
            if (_slDichVuGoc.TryGetValue(d.MaDv, out var sl)) d.SlconLai = sl;
        db.SaveChanges();
        (FormDangNhap.MaNV_DangNhap, FormDangNhap.TenNV_DangNhap, FormDangNhap.CapDoQuyen_DangNhap) = _phienGoc;
    }
}

/// <summary>
/// Test các chức năng LAB3 CÓ GHI CSDL: nhận phòng, thêm dịch vụ, thanh toán, huỷ, hoá đơn PDF,
/// xuất Excel, quản lý tài khoản. Mỗi test tự tạo và tự xoá dữ liệu tạm.
/// </summary>
public class NghiepVuGhiDbTests
{
    private static string FileTam(string duoi)
    {
        string thuMuc = Path.Combine(Path.GetTempPath(), "HotelManagementTests");
        Directory.CreateDirectory(thuMuc);
        return Path.Combine(thuMuc, $"test_{Guid.NewGuid():N}.{duoi}");
    }

    private static void BamOLuoi(Form f, string tenLuoi, string tenHamClick, string cotKhoa, string giaTriKhoa, string cotBam)
    {
        var g = Rf.Get<DataGridView>(f, tenLuoi);
        var dong = g.Rows.Cast<DataGridViewRow>().First(r => r.Cells[cotKhoa].Value?.ToString() == giaTriKhoa);
        Rf.Call(f, tenHamClick, g, new DataGridViewCellEventArgs(g.Columns[cotBam]!.Index, dong.Index));
    }

    // ===================== Nhận phòng (Đã đặt -> Đang thuê) =====================

    [DbFact]
    public void NhanPhong_DaDatSangDangThue_CapNhatCTDPVaPhong()
    {
        using var dl = new DuLieuTam();
        var ct = dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đã đặt");

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaChiTietPhieuThue { MaPhieuThue = ct.MaPt, MaPhong = ct.MaPh, MaChiTietDatPhong = ct.MaCtdp };
            Rf.Call(f, "FormSuaChiTietPhieuThue_Load", f, EventArgs.Empty);
            Rf.Get<ComboBox>(f, "cbTrangThaiCTDP").SelectedItem = "Đang thuê";
            Rf.Call(f, "btnCapNhat_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Cập nhật trạng thái thành công!"));
        });

        using var db = Db.Mo();
        Assert.Equal("Đang thuê", db.Ctdps.AsNoTracking().First(c => c.MaCtdp == "CTZ9901").TrangThai);
        Assert.Equal("Đang thuê", db.Phongs.AsNoTracking().First(p => p.MaPh == dl.Phong[0]).Ttph);
    }

    [DbFact]
    public void NhanPhong_QuaLuoiChiTietPhieu_MoDungDongVaCapNhat()
    {
        using var dl = new DuLieuTam();
        dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đã đặt");

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormChiTietPhieuThue { MaPhieuThue = DuLieuTam.MaPT };
            Rf.Call(f, "FormChiTietPhieuThue_Load", f, EventArgs.Empty);
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachCTPT");
            Assert.Equal(1, g.Rows.Count);
            Assert.NotNull(g.Rows[0].Cells["Sua"].Value);
            Assert.Null(g.Rows[0].Cells["ThanhToan"].Value);
        });
    }

    // ===================== Thêm dịch vụ cho phòng đang thuê =====================

    [DbFact]
    public void ThemDichVu_PhongDangThue_LuuCTDV_TruTonKho_CongDon()
    {
        using var dl = new DuLieuTam();
        var ct = dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đang thuê");
        using var db0 = Db.Mo();
        var dv = db0.DichVus.AsNoTracking().Where(d => d.DaXoa != true && d.SlconLai > 5).OrderBy(d => d.MaDv).First();

        void Them(string soLuong, List<string> thongBao)
        {
            Sta.Run(() =>
            {
                using var w = new MessageBoxWatcher();
                using var f = new FormThemDichVuPhongDaDung { MaChiTietDatPhong = ct.MaCtdp, MaPhong = ct.MaPh };
                Rf.Call(f, "FormThemDichVuPhongDaDung_Load", f, EventArgs.Empty);
                Rf.Get<ComboBox>(f, "cbTenDV").SelectedValue = dv.MaDv;
                Rf.Get<TextBox>(f, "txtSoLuong").Text = soLuong;
                Rf.Call(f, "btnThemDV_Click", f, EventArgs.Empty);
                thongBao.AddRange(w.Messages);
            });
        }

        var tb = new List<string>();
        Them("2", tb);
        Assert.Contains(tb, m => m.Contains("Thêm dịch vụ thành công."));
        using (var db = Db.Mo())
        {
            var ctdv = db.Ctdvs.AsNoTracking().Single(v => v.MaCtdp == ct.MaCtdp && v.MaDv == dv.MaDv);
            Assert.Equal(2, ctdv.Sl);
            Assert.Equal(2 * dv.DonGia, ctdv.ThanhTien);
            Assert.Equal(dv.SlconLai - 2, db.DichVus.AsNoTracking().First(d => d.MaDv == dv.MaDv).SlconLai);
        }

        tb.Clear();
        Them("1", tb);
        using (var db = Db.Mo())
        {
            Assert.Equal(3, db.Ctdvs.AsNoTracking().Single(v => v.MaCtdp == ct.MaCtdp && v.MaDv == dv.MaDv).Sl);
            Assert.Equal(dv.SlconLai - 3, db.DichVus.AsNoTracking().First(d => d.MaDv == dv.MaDv).SlconLai);
        }

        // Vượt tồn kho -> từ chối, không đổi dữ liệu
        tb.Clear();
        Them((dv.SlconLai!.Value).ToString(), tb);
        Assert.Contains(tb, m => m.Contains("chỉ còn"));
        using (var db = Db.Mo())
            Assert.Equal(3, db.Ctdvs.AsNoTracking().Single(v => v.MaCtdp == ct.MaCtdp && v.MaDv == dv.MaDv).Sl);

        // Danh sách dịch vụ đã dùng hiển thị đúng
        Sta.Run(() =>
        {
            using var f = new FormDanhSachDichVuPhongDaDung { MaChiTietDatPhong = ct.MaCtdp, MaPhong = ct.MaPh };
            Rf.Call(f, "FormDanhSachDichVuPhongDaDung_Load", f, EventArgs.Empty);
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachDichVu");
            Assert.Equal(1, g.Rows.Count);
            Assert.Equal(3, g.Rows[0].Cells["SL"].Value);
            Assert.Equal(3 * dv.DonGia, g.Rows[0].Cells["ThanhTien"].Value);
        });
    }

    [DbFact]
    public void ThemDichVu_KhongGioiHanSoLuong_GiuNguyenTonKho()
    {
        using var dl = new DuLieuTam();
        var ct = dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đang thuê");
        using var db0 = Db.Mo();
        var dv = db0.DichVus.AsNoTracking().FirstOrDefault(d => d.DaXoa != true && d.SlconLai < 0);
        if (dv == null) return;

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemDichVuPhongDaDung { MaChiTietDatPhong = ct.MaCtdp, MaPhong = ct.MaPh };
            Rf.Call(f, "FormThemDichVuPhongDaDung_Load", f, EventArgs.Empty);
            Rf.Get<ComboBox>(f, "cbTenDV").SelectedValue = dv.MaDv;
            Rf.Get<TextBox>(f, "txtSoLuong").Text = "50";
            Rf.Call(f, "btnThemDV_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Thêm dịch vụ thành công."));
        });

        using var db = Db.Mo();
        Assert.Equal(dv.SlconLai, db.DichVus.AsNoTracking().First(d => d.MaDv == dv.MaDv).SlconLai);
        Assert.Equal(50, db.Ctdvs.AsNoTracking().Single(v => v.MaCtdp == ct.MaCtdp).Sl);
    }

    // ===================== Thanh toán =====================

    [DbFact]
    public void ThanhToan_TaoHoaDonDungTongTien_DoiTrangThai_TraPhong_XuatPDF()
    {
        using var dl = new DuLieuTam();
        var ct = dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đang thuê", soNgay: 3);
        using (var db0 = Db.Mo())
        {
            var dv = db0.DichVus.Where(d => d.DaXoa != true).OrderBy(d => d.MaDv).First();
            db0.Ctdvs.Add(new Ctdv { MaCtdp = ct.MaCtdp, MaDv = dv.MaDv, Sl = 2, DaXoa = false });
            db0.SaveChanges();
        }
        int soHdTruoc;
        using (var db0 = Db.Mo()) soHdTruoc = db0.HoaDons.Count();

        // Thanh toán qua lưới chi tiết phiếu (bấm cột "Thanh toán", xác nhận Yes)
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher(chonYes: true);
            using var f = new FormChiTietPhieuThue { MaPhieuThue = DuLieuTam.MaPT };
            Rf.Call(f, "FormChiTietPhieuThue_Load", f, EventArgs.Empty);
            BamOLuoi(f, "dtgDanhSachCTPT", "dtgDanhSachCTPT_CellClick", "maCTDP", ct.MaCtdp, "ThanhToan");
            Assert.Contains(w.Messages, m => m.Contains("Thanh toán thành công!"));
            // sau thanh toán dòng không còn nút thao tác
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachCTPT");
            Assert.Equal("Đã thanh toán", g.Rows[0].Cells["TrangThai"].Value);
            Assert.Null(g.Rows[0].Cells["ThanhToan"].Value);
        });

        Models.HoaDon hd;
        using (var db = Db.Mo())
        {
            Assert.Equal(soHdTruoc + 1, db.HoaDons.Count());
            hd = db.HoaDons.AsNoTracking().Single(h => h.MaCtdp == ct.MaCtdp);
            var ctMoi = db.Ctdps.AsNoTracking().First(c => c.MaCtdp == ct.MaCtdp);
            decimal tienDv = db.Ctdvs.AsNoTracking().Where(v => v.MaCtdp == ct.MaCtdp).Sum(v => v.ThanhTien ?? 0);
            Assert.True(tienDv > 0);
            Assert.Equal((ctMoi.ThanhTien ?? 0) + tienDv, hd.TriGia);
            Assert.Equal("Chờ KH xác nhận", hd.TrangThai);
            Assert.Equal(dl.MaNV, hd.MaNv);
            Assert.Matches(@"^HD\d{3,}$", hd.MaHd);
            Assert.Equal("Đã thanh toán", ctMoi.TrangThai);
            Assert.Equal("Bình thường", db.Phongs.AsNoTracking().First(p => p.MaPh == dl.Phong[0]).Ttph);
        }

        // Xem hoá đơn + in PDF -> file PDF hợp lệ, hoá đơn chuyển "Đã thanh toán"
        string pdf = FileTam("pdf");
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher { TenFileLuu = pdf };
            using var f = new FormHoaDon(hd.MaHd);
            Rf.Call(f, "FormHoaDon_Load", f, EventArgs.Empty);
            Assert.Equal("Tổng tiền: " + (string)Rf.Call(f, "DinhDangTien", hd.TriGia!.Value)!, Rf.Get<Label>(f, "lbTongTriGia").Text);
            Assert.Equal(1, Rf.Get<DataGridView>(f, "dtgDichVuDaDung").Rows.Count);
            Assert.Equal("3 ngày", Rf.Get<TextBox>(f, "txtSoNgayThuePhong").Text);
            Rf.Call(f, "Printer_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Xuất hóa đơn PDF thành công!"));
        });
        Assert.True(File.Exists(pdf));
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(pdf), 0, 4));
        File.Delete(pdf);
        using (var db = Db.Mo())
            Assert.Equal("Đã thanh toán", db.HoaDons.AsNoTracking().First(h => h.MaHd == hd.MaHd).TrangThai);
    }

    [DbFact]
    public void ThanhToan_PhongConLuotDatKhac_GiuTrangThaiDaDat()
    {
        using var dl = new DuLieuTam();
        dl.TaoCtdp("CTZ9901", dl.Phong[1], "Đang thuê");
        dl.TaoCtdp("CTZ9902", dl.Phong[1], "Đã đặt", lechNgay: 5);

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormChiTietPhieuThue { MaPhieuThue = DuLieuTam.MaPT };
            Rf.Call(f, "TinhTienVaThanhToanHoaDon", "CTZ9901");
            Assert.Contains(w.Messages, m => m.Contains("Thanh toán thành công!"));
        });

        using var db = Db.Mo();
        Assert.Equal("Đã đặt", db.Phongs.AsNoTracking().First(p => p.MaPh == dl.Phong[1]).Ttph);
    }

    [DbFact]
    public void ThanhToan_ChuaDangNhap_KhongTaoHoaDon()
    {
        using var dl = new DuLieuTam();
        dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đang thuê");
        FormDangNhap.MaNV_DangNhap = "";

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormChiTietPhieuThue { MaPhieuThue = DuLieuTam.MaPT };
            Rf.Call(f, "TinhTienVaThanhToanHoaDon", "CTZ9901");
            Assert.Contains(w.Messages, m => m.Contains("Chưa xác định được nhân viên đang đăng nhập"));
        });

        using var db = Db.Mo();
        Assert.False(db.HoaDons.Any(h => h.MaCtdp == "CTZ9901"));
        Assert.Equal("Đang thuê", db.Ctdps.AsNoTracking().First(c => c.MaCtdp == "CTZ9901").TrangThai);
    }

    // ===================== Huỷ phòng đã đặt =====================

    [DbFact]
    public void HuyPhong_QuaLuoi_XacNhanYes_XoaMemVaTraPhong()
    {
        using var dl = new DuLieuTam();
        dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đã đặt");

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher(chonYes: true);
            using var f = new FormChiTietPhieuThue { MaPhieuThue = DuLieuTam.MaPT };
            Rf.Call(f, "FormChiTietPhieuThue_Load", f, EventArgs.Empty);
            BamOLuoi(f, "dtgDanhSachCTPT", "dtgDanhSachCTPT_CellClick", "maCTDP", "CTZ9901", "Huy");
            Assert.Contains(w.Messages, m => m.Contains("đã được huỷ"));
            Assert.Equal(0, Rf.Get<DataGridView>(f, "dtgDanhSachCTPT").Rows.Count);
        });

        using var db = Db.Mo();
        Assert.True(db.Ctdps.AsNoTracking().First(c => c.MaCtdp == "CTZ9901").DaXoa);
        Assert.Equal("Bình thường", db.Phongs.AsNoTracking().First(p => p.MaPh == dl.Phong[0]).Ttph);
    }

    [DbFact]
    public void HuyPhong_XacNhanNo_KhongThayDoi()
    {
        using var dl = new DuLieuTam();
        dl.TaoCtdp("CTZ9901", dl.Phong[0], "Đã đặt");

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher(chonYes: false);
            using var f = new FormChiTietPhieuThue { MaPhieuThue = DuLieuTam.MaPT };
            Rf.Call(f, "FormChiTietPhieuThue_Load", f, EventArgs.Empty);
            BamOLuoi(f, "dtgDanhSachCTPT", "dtgDanhSachCTPT_CellClick", "maCTDP", "CTZ9901", "Huy");
        });

        using var db = Db.Mo();
        Assert.NotEqual(true, db.Ctdps.AsNoTracking().First(c => c.MaCtdp == "CTZ9901").DaXoa);
        Assert.Equal("Đã đặt", db.Phongs.AsNoTracking().First(p => p.MaPh == dl.Phong[0]).Ttph);
    }

    // ===================== Xuất Excel =====================

    [DbFact]
    public void XuatExcel_BaDanhSach_FileDungSoDong()
    {
        var cacForm = new (Func<Form> Tao, string HamLoad, string Luoi)[]
        {
            (() => new FormDanhSachPhieuThue(), "FormDanhSachPhieuThue_Load", "dtgDanhSachPhieuThue"),
            (() => new FormDanhSachHoaDon(), "FormDanhSachHoaDon_Load", "dtgDanhSachHoaDon"),
            (() => new FormDanhSachTaiKhoan(), "FormDanhSachTaiKhoan_Load", "dtgDanhSachTaiKhoan"),
        };

        foreach (var c in cacForm)
        {
            string file = FileTam("xlsx");
            int soDong = 0;
            Sta.Run(() =>
            {
                using var w = new MessageBoxWatcher { TenFileLuu = file };
                using var f = c.Tao();
                Rf.Call(f, c.HamLoad, f, EventArgs.Empty);
                soDong = Rf.Get<DataGridView>(f, c.Luoi).Rows.Count;
                Rf.Call(f, "btnXuatExcel_Click", f, EventArgs.Empty);
                Assert.Contains(w.Messages, m => m.Contains("Xuất dữ liệu thành công!"));
            });
            Assert.True(File.Exists(file), f(c.Luoi) + ": không tạo được file");
            using (var wb = new XLWorkbook(file))
            {
                var ws = wb.Worksheets.First();
                Assert.Equal(soDong + 1, ws.LastRowUsed()!.RowNumber());
                Assert.False(ws.Cell(1, 1).IsEmpty());
            }
            File.Delete(file);
        }

        static string f(string s) => s;
    }

    // ===================== Tài khoản =====================

    [DbFact]
    public void TaiKhoan_Them_Sua_Xoa_ThemLai()
    {
        using var dl = new DuLieuTam();
        FormDangNhap.MaNV_DangNhap = "";

        // Thêm
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemTaiKhoan();
            Rf.Call(f, "FormThemTaiKhoan_Load", f, EventArgs.Empty);
            Rf.Get<ComboBox>(f, "cbMaNV").SelectedValue = dl.MaNV;
            Rf.Get<TextBox>(f, "txtTenTK").Text = "tz_lab3";
            Rf.Get<TextBox>(f, "txtMatKhau").Text = "123";
            Rf.Get<ComboBox>(f, "cbCapDoQuyen").SelectedItem = "2";
            Rf.Call(f, "btnThemMoi_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Thêm tài khoản thành công!"));
            Assert.Equal(DialogResult.OK, f.DialogResult);
        });
        using (var db = Db.Mo())
        {
            var tk = db.TaiKhoans.AsNoTracking().Single(t => t.TenTk == "tz_lab3");
            Assert.Equal(("123", 2, dl.MaNV), (tk.Password, tk.CapDoQuyen, tk.MaNv));
        }

        // Sửa: đổi tên, mật khẩu, quyền
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaTaiKhoan("tz_lab3");
            Rf.Call(f, "FormSuaTaiKhoan_Load", f, EventArgs.Empty);
            Assert.Equal("2", Rf.Get<ComboBox>(f, "cbCapDoQuyen").SelectedItem);
            Rf.Get<TextBox>(f, "txtTenTK").Text = "tz_lab3_moi";
            Rf.Get<TextBox>(f, "txtMatKhau").Text = "456";
            Rf.Get<ComboBox>(f, "cbCapDoQuyen").SelectedItem = "1";
            Rf.Call(f, "btnCapNhat_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Cập nhật tài khoản thành công!"));
        });
        using (var db = Db.Mo())
        {
            Assert.False(db.TaiKhoans.Any(t => t.TenTk == "tz_lab3"));
            var tk = db.TaiKhoans.AsNoTracking().Single(t => t.TenTk == "tz_lab3_moi");
            Assert.Equal(("456", 1), (tk.Password, tk.CapDoQuyen));
        }

        // Sửa trùng tên tài khoản khác -> báo lỗi
        string tenKhac;
        using (var db = Db.Mo()) tenKhac = db.TaiKhoans.First(t => t.DaXoa != true && t.TenTk != "tz_lab3_moi").TenTk;
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaTaiKhoan("tz_lab3_moi");
            Rf.Call(f, "FormSuaTaiKhoan_Load", f, EventArgs.Empty);
            Rf.Get<TextBox>(f, "txtTenTK").Text = tenKhac;
            Rf.Call(f, "btnCapNhat_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Tên tài khoản đã tồn tại!"));
        });

        // Xoá qua lưới (Yes)
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher(chonYes: true);
            using var f = new FormDanhSachTaiKhoan();
            Rf.Call(f, "FormDanhSachTaiKhoan_Load", f, EventArgs.Empty);
            BamOLuoi(f, "dtgDanhSachTaiKhoan", "dtgDanhSachTaiKhoan_CellClick", "TenTK", "tz_lab3_moi", "Xoa");
            Assert.Contains(w.Messages, m => m.Contains("Xóa tài khoản thành công."));
            Assert.DoesNotContain(Rf.Get<DataGridView>(f, "dtgDanhSachTaiKhoan").Rows.Cast<DataGridViewRow>(),
                r => (string)r.Cells["TenTK"].Value! == "tz_lab3_moi");
        });
        using (var db = Db.Mo())
            Assert.True(db.TaiKhoans.AsNoTracking().Single(t => t.TenTk == "tz_lab3_moi").DaXoa);

        // Thêm lại tên đã xoá -> khôi phục
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemTaiKhoan();
            Rf.Call(f, "FormThemTaiKhoan_Load", f, EventArgs.Empty);
            Rf.Get<TextBox>(f, "txtTenTK").Text = "tz_lab3_moi";
            Rf.Get<TextBox>(f, "txtMatKhau").Text = "789";
            Rf.Call(f, "btnThemMoi_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Thêm tài khoản thành công!"));
        });
        using (var db = Db.Mo())
        {
            var tk = db.TaiKhoans.AsNoTracking().Single(t => t.TenTk == "tz_lab3_moi");
            Assert.Equal(("789", false), (tk.Password, tk.DaXoa ?? false));
        }
    }

    [DbFact]
    public void TaiKhoan_KhongXoaDuocTaiKhoanDangDangNhap()
    {
        using var dl = new DuLieuTam();
        using (var db = Db.Mo())
        {
            db.TaiKhoans.Add(new Models.TaiKhoan { TenTk = "tz_lab3_dn", Password = "1", CapDoQuyen = 1, MaNv = dl.MaNV, DaXoa = false });
            db.SaveChanges();
        }
        FormDangNhap.MaNV_DangNhap = dl.MaNV;

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher(chonYes: true);
            using var f = new FormDanhSachTaiKhoan();
            Rf.Call(f, "FormDanhSachTaiKhoan_Load", f, EventArgs.Empty);
            BamOLuoi(f, "dtgDanhSachTaiKhoan", "dtgDanhSachTaiKhoan_CellClick", "TenTK", "tz_lab3_dn", "Xoa");
            Assert.Contains(w.Messages, m => m.Contains("Không thể xóa tài khoản đang đăng nhập!"));
        });

        using var db2 = Db.Mo();
        Assert.NotEqual(true, db2.TaiKhoans.AsNoTracking().Single(t => t.TenTk == "tz_lab3_dn").DaXoa);
    }

    [DbFact]
    public void TaiKhoan_CapQuyenNgoai1Den3_BiTuChoi()
    {
        using var dl = new DuLieuTam();
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemTaiKhoan();
            Rf.Call(f, "FormThemTaiKhoan_Load", f, EventArgs.Empty);
            var cb = Rf.Get<ComboBox>(f, "cbCapDoQuyen");
            cb.DropDownStyle = ComboBoxStyle.DropDown;
            cb.Items.Add("0");
            cb.SelectedItem = "0";
            Rf.Get<TextBox>(f, "txtTenTK").Text = "tz_lab3_q0";
            Rf.Get<TextBox>(f, "txtMatKhau").Text = "1";
            Rf.Call(f, "btnThemMoi_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Cấp độ quyền không hợp lệ!"));
        });
        using var db = Db.Mo();
        Assert.False(db.TaiKhoans.Any(t => t.TenTk == "tz_lab3_q0"));
    }
}

/// <summary>Smoke test: mở từng màn hình từ menu FormMain như người dùng bấm.</summary>
public class FormMainTests
{
    [DbFact]
    public void FormMain_BamTungMenu_MoDungManHinh_KhongLoi()
    {
        var phien = (FormDangNhap.MaNV_DangNhap, FormDangNhap.TenNV_DangNhap, FormDangNhap.CapDoQuyen_DangNhap);
        using (var db = Db.Mo())
        {
            var tk = db.TaiKhoans.Include(t => t.MaNvNavigation).First(t => t.DaXoa != true && t.CapDoQuyen == 3);
            FormDangNhap.MaNV_DangNhap = tk.MaNv;
            FormDangNhap.TenNV_DangNhap = tk.MaNvNavigation.TenNv;
            FormDangNhap.CapDoQuyen_DangNhap = tk.CapDoQuyen;
        }

        try
        {
            var loi = new List<string>();
            Sta.Run(() =>
            {
                using var w = new MessageBoxWatcher();
                using var main = new FormMain { StartPosition = FormStartPosition.Manual, Location = new Point(-5000, -5000) };
                main.Show();
                Application.DoEvents();
                var panel = Rf.Get<Panel>(main, "panelMain");

                var mongDoi = new Dictionary<string, Type>
                {
                    ["button9_Click"] = typeof(FormDanhSachPhieuThue),
                    ["button10_Click"] = typeof(FormDanhSachHoaDon),
                    ["button5_Click"] = typeof(FormDanhSachTaiKhoan),
                    ["button13_Click"] = typeof(ThongKe.FormThongKe),
                };
                var tatCa = typeof(FormMain).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Where(m => m.Name.EndsWith("_Click") && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(EventArgs))
                    .Select(m => m.Name).ToList();

                foreach (var ham in tatCa)
                {
                    w.Messages.Clear();
                    Rf.Call(main, ham, main, EventArgs.Empty);
                    Application.DoEvents();
                    if (w.Messages.Count > 0) loi.Add($"{ham}: {string.Join(" | ", w.Messages)}");
                    if (panel.Controls.Count != 1) loi.Add($"{ham}: panelMain có {panel.Controls.Count} control");
                    if (panel.Controls.Count == 1 && panel.Controls[0] is Label lb && lb.Text.Contains("đang được xây dựng"))
                        loi.Add($"{ham}: vẫn hiện 'đang được xây dựng'");
                    if (mongDoi.TryGetValue(ham, out var loai) && (panel.Controls.Count == 0 || panel.Controls[0].GetType() != loai))
                        loi.Add($"{ham}: không mở {loai.Name}");
                }
                main.Close();
            });
            Assert.True(loi.Count == 0, string.Join("\n", loi));
        }
        finally
        {
            (FormDangNhap.MaNV_DangNhap, FormDangNhap.TenNV_DangNhap, FormDangNhap.CapDoQuyen_DangNhap) = phien;
        }
    }
}
