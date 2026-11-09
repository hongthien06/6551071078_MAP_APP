using HotelManagement.HoaDon;
using HotelManagement.PhieuThue;
using HotelManagement.TaiKhoan;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelManagement.Tests;

/// <summary>
/// Test chức năng LAB3 trên CSDL LocalDB thật — CHỈ ĐỌC, không có test nào ghi dữ liệu.
/// MessageBox được tự đóng và nội dung được ghi lại để kiểm tra.
/// </summary>
public class NghiepVuDocDbTests
{
    // ===================== Thêm dịch vụ phòng =====================

    [Fact]
    public void ThemDichVu_ChuaTruyenMaCTDP_BaoLoiVaKhoaNutThem()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemDichVuPhongDaDung();
            Rf.Call(f, "FormThemDichVuPhongDaDung_Load", f, EventArgs.Empty);
            Assert.False(Rf.Get<Button>(f, "btnThemDV").Enabled);
            Assert.Contains(w.Messages, m => m.Contains("Chưa xác định được chi tiết phiếu thuê cần thêm dịch vụ."));
        });
    }

    [DbFact]
    public void ThemDichVu_Load_NapDichVuChuaXoaVaTinhThanhTien()
    {
        using var db = Db.Mo();
        var ctdp = db.Ctdps.AsNoTracking().First(c => c.DaXoa != true);
        var dsDv = db.DichVus.AsNoTracking().Where(d => d.DaXoa != true).OrderBy(d => d.TenDv).ToList();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemDichVuPhongDaDung { MaChiTietDatPhong = ctdp.MaCtdp, MaPhong = ctdp.MaPh };
            Rf.Call(f, "FormThemDichVuPhongDaDung_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);

            var cb = Rf.Get<ComboBox>(f, "cbTenDV");
            Assert.Equal(dsDv.Count, cb.Items.Count);
            Assert.Equal(-1, cb.SelectedIndex);
            Assert.Equal("Mã CTDP: " + ctdp.MaCtdp, Rf.Get<Label>(f, "lbSoCTPhieu").Text);
            Assert.Equal("Số phòng: " + ctdp.MaPh, Rf.Get<Label>(f, "lbSoPhong").Text);

            // Chọn dịch vụ đầu tiên -> hiện mã + đơn giá
            cb.SelectedIndex = 0;
            Assert.Equal(dsDv[0].MaDv, Rf.Get<TextBox>(f, "txtMaDV").Text);
            Assert.Equal(dsDv[0].DonGia.ToString("N0"), Rf.Get<TextBox>(f, "txtDonGia").Text);

            // Nhập số lượng -> thành tiền = SL * đơn giá
            var txtSL = Rf.Get<TextBox>(f, "txtSoLuong");
            txtSL.Text = "3";
            Assert.Equal("Thành tiền: " + (3 * dsDv[0].DonGia).ToString("N0"), Rf.Get<Label>(f, "lbThanhTien").Text);
            txtSL.Text = "0";
            Assert.Equal("Thành tiền: 0", Rf.Get<Label>(f, "lbThanhTien").Text);
        });
    }

    [DbFact]
    public void ThemDichVu_ChuaChonDichVu_BaoVuiLongChon()
    {
        using var db = Db.Mo();
        var ctdp = db.Ctdps.AsNoTracking().First(c => c.DaXoa != true);
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemDichVuPhongDaDung { MaChiTietDatPhong = ctdp.MaCtdp, MaPhong = ctdp.MaPh };
            Rf.Call(f, "FormThemDichVuPhongDaDung_Load", f, EventArgs.Empty);
            Rf.Call(f, "btnThemDV_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Vui lòng chọn dịch vụ."));
        });
    }

    [DbFact]
    public void ThemDichVu_SoLuongKhongHopLe_BaoLoi()
    {
        using var db = Db.Mo();
        var ctdp = db.Ctdps.AsNoTracking().First(c => c.DaXoa != true);
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemDichVuPhongDaDung { MaChiTietDatPhong = ctdp.MaCtdp, MaPhong = ctdp.MaPh };
            Rf.Call(f, "FormThemDichVuPhongDaDung_Load", f, EventArgs.Empty);
            Rf.Get<ComboBox>(f, "cbTenDV").SelectedIndex = 0;
            foreach (var sl in new[] { "", "0", "000" })
            {
                Rf.Get<TextBox>(f, "txtSoLuong").Text = sl;
                Rf.Call(f, "btnThemDV_Click", f, EventArgs.Empty);
            }
            Assert.Equal(3, w.Messages.Count(m => m.Contains("Số lượng không hợp lệ.")));
        });
    }

    [DbFact]
    public void ThemDichVu_PhongKhongDangThue_TuChoiVaKhongGhiDb()
    {
        using var db = Db.Mo();
        var ctdp = db.Ctdps.AsNoTracking().FirstOrDefault(c => c.DaXoa != true && c.TrangThai != "Đang thuê");
        Assert.NotNull(ctdp);
        int soCtdvTruoc = db.Ctdvs.Count();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemDichVuPhongDaDung { MaChiTietDatPhong = ctdp!.MaCtdp, MaPhong = ctdp.MaPh };
            Rf.Call(f, "FormThemDichVuPhongDaDung_Load", f, EventArgs.Empty);
            Rf.Get<ComboBox>(f, "cbTenDV").SelectedIndex = 0;
            Rf.Get<TextBox>(f, "txtSoLuong").Text = "1";
            Rf.Call(f, "btnThemDV_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Chỉ có thể thêm dịch vụ cho phòng đang thuê!"));
        });

        using var db2 = Db.Mo();
        Assert.Equal(soCtdvTruoc, db2.Ctdvs.Count());
    }

    // ===================== Sửa chi tiết phiếu thuê =====================

    [Fact]
    public void SuaChiTietPhieu_ChuaTruyenThamSo_BaoLoiVaKhoaNut()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaChiTietPhieuThue();
            Rf.Call(f, "FormSuaChiTietPhieuThue_Load", f, EventArgs.Empty);
            Assert.False(Rf.Get<Button>(f, "btnCapNhat").Enabled);
            Assert.Contains(w.Messages, m => m.Contains("Chưa xác định được chi tiết phiếu thuê cần cập nhật."));
        });
    }

    [DbFact]
    public void SuaChiTietPhieu_Load_HienMaCTDP()
    {
        using var db = Db.Mo();
        var ctdp = db.Ctdps.AsNoTracking().Where(c => c.DaXoa != true).OrderBy(c => c.MaCtdp).First();
        string mongDoi = db.Ctdps.AsNoTracking()
            .Where(c => c.MaPt == ctdp.MaPt && c.MaPh == ctdp.MaPh && c.DaXoa != true)
            .OrderByDescending(c => c.MaCtdp).First().MaCtdp;

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaChiTietPhieuThue { MaPhieuThue = ctdp.MaPt, MaPhong = ctdp.MaPh };
            Rf.Call(f, "FormSuaChiTietPhieuThue_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);
            Assert.True(Rf.Get<Button>(f, "btnCapNhat").Enabled);
            Assert.Equal("Cập nhật trạng thái CTDP " + mongDoi, Rf.Get<Label>(f, "lbMaCTDP").Text);
        });
    }

    [DbFact]
    public void SuaChiTietPhieu_KhongTimThayPhong_BaoLoi()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaChiTietPhieuThue { MaPhieuThue = "ZZZZZ", MaPhong = "ZZZZZ" };
            Rf.Call(f, "FormSuaChiTietPhieuThue_Load", f, EventArgs.Empty);
            Assert.False(Rf.Get<Button>(f, "btnCapNhat").Enabled);
            Assert.Contains(w.Messages, m => m.Contains("Không tìm thấy chi tiết phiếu thuê của phòng ZZZZZ."));
        });
    }

    // ===================== Danh sách dịch vụ đã dùng =====================

    [DbFact]
    public void DanhSachDichVuDaDung_Load_DungSoDongVaThanhTien()
    {
        using var db = Db.Mo();
        var nhom = db.Ctdvs.AsNoTracking().Where(v => v.DaXoa != true)
            .GroupBy(v => v.MaCtdp).Select(g => new { MaCtdp = g.Key, SoDong = g.Count() }).First();
        var ctdp = db.Ctdps.AsNoTracking().First(c => c.MaCtdp == nhom.MaCtdp);

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormDanhSachDichVuPhongDaDung { MaChiTietDatPhong = ctdp.MaCtdp, MaPhong = ctdp.MaPh };
            Rf.Call(f, "FormDanhSachDichVuPhongDaDung_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachDichVu");
            Assert.Equal(nhom.SoDong, g.Rows.Count);
            Assert.Equal(new[] { "MaDV", "TenDV", "DonGia", "SL", "ThanhTien" },
                g.Columns.Cast<DataGridViewColumn>().Select(c => c.Name).ToArray());
        });
    }

    // ===================== Chi tiết phiếu thuê =====================

    [DbFact]
    public void ChiTietPhieuThue_Load_IconHanhDongTheoTrangThai()
    {
        using var db = Db.Mo();
        var pt = db.PhieuThues.AsNoTracking().Include(p => p.MaKhNavigation)
            .First(p => p.Ctdps.Any(c => c.DaXoa != true));
        var ds = db.Ctdps.AsNoTracking().Where(c => c.MaPt == pt.MaPt && c.DaXoa != true).OrderBy(c => c.MaCtdp).ToList();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormChiTietPhieuThue { MaPhieuThue = pt.MaPt };
            Rf.Call(f, "FormChiTietPhieuThue_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);

            Assert.Equal("Chi tiết phiếu thuê " + pt.MaPt, Rf.Get<Label>(f, "lbSoPhieu").Text);
            Assert.Equal(pt.MaKhNavigation.TenKh, Rf.Get<Label>(f, "lbHoTenKH").Text);

            var g = Rf.Get<DataGridView>(f, "dtgDanhSachCTPT");
            Assert.Equal(ds.Count, g.Rows.Count);
            for (int i = 0; i < ds.Count; i++)
            {
                var r = g.Rows[i];
                bool daDat = ds[i].TrangThai == "Đã đặt";
                bool dangThue = ds[i].TrangThai == "Đang thuê";
                Assert.Equal(ds[i].MaCtdp, r.Cells["maCTDP"].Value);
                Assert.Equal(daDat, r.Cells["Huy"].Value != null);
                Assert.Equal(daDat, r.Cells["Sua"].Value != null);
                Assert.Equal(dangThue, r.Cells["DichVu"].Value != null);
                Assert.Equal(dangThue, r.Cells["ThanhToan"].Value != null);
            }
        });
    }

    [Fact]
    public void ChiTietPhieuThue_ChuaTruyenMaPhieu_BaoLoi()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormChiTietPhieuThue();
            Rf.Call(f, "FormChiTietPhieuThue_Load", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Chưa xác định được phiếu thuê cần xem."));
        });
    }

    // ===================== Danh sách phiếu thuê =====================

    [DbFact]
    public void DanhSachPhieuThue_Load_VaTimKiemKhongPhanBietHoaThuong()
    {
        using var db = Db.Mo();
        int tong = db.PhieuThues.Count(p => p.DaXoa != true);
        var mau = db.PhieuThues.AsNoTracking().Where(p => p.DaXoa != true).OrderBy(p => p.MaPt).First();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormDanhSachPhieuThue();
            Rf.Call(f, "FormDanhSachPhieuThue_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachPhieuThue");
            Assert.Equal(tong, g.Rows.Count);

            Rf.Get<TextBox>(f, "txtTimKiem").Text = mau.MaPt.ToLowerInvariant();
            Assert.True(g.Rows.Count >= 1);
            Assert.All(g.Rows.Cast<DataGridViewRow>(), r =>
                Assert.True(r.Cells["MaPT"].Value!.ToString()!.Contains(mau.MaPt, StringComparison.OrdinalIgnoreCase)
                         || r.Cells["TenKH"].Value!.ToString()!.Contains(mau.MaPt, StringComparison.OrdinalIgnoreCase)));

            Rf.Get<TextBox>(f, "txtTimKiem").Text = "khong-ton-tai-xyz";
            Assert.Equal(0, g.Rows.Count);
        });
    }

    // ===================== Hóa đơn =====================

    [DbFact]
    public void DanhSachHoaDon_Load_DungSoDong_VaLocTheoMa()
    {
        using var db = Db.Mo();
        int tong = db.HoaDons.Count(h => h.DaXoa != true);
        string maDau = db.HoaDons.Where(h => h.DaXoa != true).OrderBy(h => h.MaHd).Select(h => h.MaHd).First();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormDanhSachHoaDon();
            Rf.Call(f, "FormDanhSachHoaDon_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachHoaDon");
            Assert.Equal(tong, g.Rows.Count);

            Rf.Get<TextBox>(f, "txtTimKiem").Text = maDau;
            Assert.Contains(g.Rows.Cast<DataGridViewRow>(), r => (string)r.Cells["MaHD"].Value! == maDau);
        });
    }

    [DbFact]
    public void HoaDon_MaKhongTonTai_BaoKhongTimThay()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormHoaDon("ZZZZZ");
            Rf.Call(f, "FormHoaDon_Load", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Không tìm thấy hóa đơn!"));
        });
    }

    [DbFact]
    public void HoaDon_Load_HienThongTinHoaDon()
    {
        using var db = Db.Mo();
        var hd = db.HoaDons.AsNoTracking().Include(h => h.MaCtdpNavigation)
            .Where(h => h.DaXoa != true).OrderBy(h => h.MaHd).First();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormHoaDon(hd.MaHd);
            Rf.Call(f, "FormHoaDon_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);
            Assert.Equal(hd.MaHd, Rf.Get<TextBox>(f, "txtMaHoaDon").Text);
            Assert.Equal(hd.MaCtdpNavigation.MaPh, Rf.Get<TextBox>(f, "txtMaPhong").Text);
            Assert.StartsWith("Tổng tiền: ", Rf.Get<Label>(f, "lbTongTriGia").Text);
        });
    }

    /// <summary>
    /// Tổng tiền hiển thị trên FormHoaDon / FormDanhSachHoaDon được TÍNH LẠI từ đơn giá dịch vụ hiện tại,
    /// còn khi thanh toán (FormChiTietPhieuThue) thì TriGia lưu vào DB lấy từ CTDV.ThanhTien.
    /// Test này so sánh hai cách tính trên dữ liệu thật.
    /// </summary>
    [DbFact]
    public void HoaDon_TongTienHienThi_KhopTriGiaDaLuu()
    {
        using var db = Db.Mo();
        var dsHd = db.HoaDons.AsNoTracking().Where(h => h.DaXoa != true && h.TriGia != null)
            .Select(h => new { h.MaHd, TriGia = h.TriGia!.Value }).ToList();
        var lech = new List<string>();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();

            // Lưới danh sách hóa đơn
            using (var ds = new FormDanhSachHoaDon())
            {
                Rf.Call(ds, "FormDanhSachHoaDon_Load", ds, EventArgs.Empty);
                var g = Rf.Get<DataGridView>(ds, "dtgDanhSachHoaDon");
                foreach (DataGridViewRow r in g.Rows)
                {
                    var hd = dsHd.FirstOrDefault(x => x.MaHd == (string)r.Cells["MaHD"].Value!);
                    if (hd != null && (decimal)r.Cells["TriGia"].Value! != hd.TriGia)
                        lech.Add($"Danh sách {hd.MaHd}: {r.Cells["TriGia"].Value:N0} ≠ {hd.TriGia:N0}");
                }
            }

            // Màn hình chi tiết hóa đơn
            foreach (var hd in dsHd)
            {
                using var f = new FormHoaDon(hd.MaHd);
                Rf.Call(f, "FormHoaDon_Load", f, EventArgs.Empty);
                string mongDoi = "Tổng tiền: " + (string)Rf.Call(f, "DinhDangTien", hd.TriGia)!;
                string thucTe = Rf.Get<Label>(f, "lbTongTriGia").Text;
                if (thucTe != mongDoi) lech.Add($"FormHoaDon {hd.MaHd}: '{thucTe}' ≠ '{mongDoi}'");
            }
            Assert.Empty(w.Messages);
        });

        Assert.True(lech.Count == 0, "Hóa đơn bị lệch tổng tiền: " + string.Join("; ", lech));
    }

    [DbFact]
    public void HoaDon_SoNgayThue_CoDonVi()
    {
        using var db = Db.Mo();
        string maHd = db.HoaDons.Where(h => h.DaXoa != true).OrderBy(h => h.MaHd).Select(h => h.MaHd).First();
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormHoaDon(maHd);
            Rf.Call(f, "FormHoaDon_Load", f, EventArgs.Empty);
            string s = Rf.Get<TextBox>(f, "txtSoNgayThuePhong").Text;
            Assert.True(s.EndsWith(" ngày") || s.EndsWith(" giờ"), "Số ngày thiếu đơn vị: " + s);
        });
    }

    [DbFact]
    public void SuaChiTietPhieu_ChonDungDongTheoMaCTDP()
    {
        using var db = Db.Mo();
        var ctdp = db.Ctdps.AsNoTracking().Where(c => c.DaXoa != true).OrderBy(c => c.MaCtdp).First();
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaChiTietPhieuThue { MaPhieuThue = ctdp.MaPt, MaPhong = ctdp.MaPh, MaChiTietDatPhong = ctdp.MaCtdp };
            Rf.Call(f, "FormSuaChiTietPhieuThue_Load", f, EventArgs.Empty);
            Assert.Equal("Cập nhật trạng thái CTDP " + ctdp.MaCtdp, Rf.Get<Label>(f, "lbMaCTDP").Text);
        });
    }

    [DbFact]
    public void SuaChiTietPhieu_DaDatSangDaThanhToan_BiChanVaKhongGhiDb()
    {
        using var db = Db.Mo();
        var ctdp = db.Ctdps.AsNoTracking().FirstOrDefault(c => c.DaXoa != true && c.TrangThai == "Đã đặt");
        if (ctdp == null) return; // Không có dữ liệu "Đã đặt" để kiểm tra
        string ttPhongTruoc = db.Phongs.AsNoTracking().First(p => p.MaPh == ctdp.MaPh).Ttph;
        int soHdTruoc = db.HoaDons.Count();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaChiTietPhieuThue { MaPhieuThue = ctdp.MaPt, MaPhong = ctdp.MaPh, MaChiTietDatPhong = ctdp.MaCtdp };
            Rf.Call(f, "FormSuaChiTietPhieuThue_Load", f, EventArgs.Empty);
            Rf.Get<ComboBox>(f, "cbTrangThaiCTDP").SelectedItem = "Đã thanh toán";
            Rf.Call(f, "btnCapNhat_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Không thể chuyển trực tiếp sang"));
        });

        using var db2 = Db.Mo();
        Assert.Equal("Đã đặt", db2.Ctdps.AsNoTracking().First(c => c.MaCtdp == ctdp.MaCtdp).TrangThai);
        Assert.Equal(ttPhongTruoc, db2.Phongs.AsNoTracking().First(p => p.MaPh == ctdp.MaPh).Ttph);
        Assert.Equal(soHdTruoc, db2.HoaDons.Count());
    }

    // ===================== Tài khoản =====================

    [DbFact]
    public void DanhSachTaiKhoan_Load_DungSoDongVaTenQuyen()
    {
        using var db = Db.Mo();
        int tong = db.TaiKhoans.Count(t => t.DaXoa != true);

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormDanhSachTaiKhoan();
            Rf.Call(f, "LoadDanhSachTaiKhoan", "");
            Assert.Empty(w.Messages);
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachTaiKhoan");
            Assert.Equal(tong, g.Rows.Count);
            Assert.All(g.Rows.Cast<DataGridViewRow>(), r => Assert.StartsWith("Cấp ", (string)r.Cells["CapDoQuyen"].Value!));

            Rf.Call(f, "LoadDanhSachTaiKhoan", "admin");
            Assert.All(g.Rows.Cast<DataGridViewRow>(), r =>
                Assert.Contains("admin", (string)r.Cells["TenTK"].Value!, StringComparison.OrdinalIgnoreCase));
        });
    }

    [DbFact]
    public void CSDL_CotMaHD_Va_Trigger_DaNangCap()
    {
        using var db = Db.Mo();
        int doDai = db.Database.SqlQueryRaw<int>(
            "SELECT CAST(max_length / 2 AS int) AS Value FROM sys.columns WHERE object_id = OBJECT_ID('HoaDon') AND name = 'MaHD'").Single();
        Assert.True(doDai >= 10, $"Cột HoaDon.MaHD mới dài {doDai} ký tự — hãy chạy Database/NangCap_MaHD_10KyTu.sql");
        int triggerMoi = db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS Value FROM sys.sql_modules WHERE object_id = OBJECT_ID('CapNhatGiaTriHoaDon') AND definition LIKE '%@MaHD NVARCHAR(10)%'").Single();
        Assert.Equal(1, triggerMoi);
    }

    [Fact]
    public void ThemTaiKhoan_BoTrongThongTin_BaoVuiLongDienDu()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemTaiKhoan();
            Rf.Call(f, "btnThemMoi_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Vui lòng điền đầy đủ thông tin!"));
        });
    }

    [DbFact]
    public void ThemTaiKhoan_Load_NapNhanVienVaMacDinhQuyen1()
    {
        using var db = Db.Mo();
        int soNv = db.NhanViens.Count(n => n.DaXoa != true);

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemTaiKhoan();
            Rf.Call(f, "FormThemTaiKhoan_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);
            var cbNv = Rf.Get<ComboBox>(f, "cbMaNV");
            Assert.Equal(soNv, cbNv.Items.Count);
            Assert.False(string.IsNullOrEmpty(cbNv.SelectedValue?.ToString()));
            Assert.Equal("1", Rf.Get<ComboBox>(f, "cbCapDoQuyen").SelectedItem?.ToString());
        });
    }

    [DbFact]
    public void ThemTaiKhoan_TenDaTonTai_BaoLoiVaKhongGhiDb()
    {
        using var db = Db.Mo();
        var tk = db.TaiKhoans.AsNoTracking().First(t => t.DaXoa != true);
        int truoc = db.TaiKhoans.Count();

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThemTaiKhoan();
            Rf.Call(f, "FormThemTaiKhoan_Load", f, EventArgs.Empty);
            Rf.Get<TextBox>(f, "txtTenTK").Text = tk.TenTk;
            Rf.Get<TextBox>(f, "txtMatKhau").Text = "abc";
            Rf.Call(f, "btnThemMoi_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Tên tài khoản đã tồn tại!"));
        });

        using var db2 = Db.Mo();
        Assert.Equal(truoc, db2.TaiKhoans.Count());
    }

    [Fact]
    public void SuaTaiKhoan_KhongTruyenTenTK_BaoLoi()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaTaiKhoan();
            Rf.Call(f, "FormSuaTaiKhoan_Load", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Không xác định được tài khoản cần cập nhật!"));
        });
    }

    [DbFact]
    public void SuaTaiKhoan_Load_HienDungThongTin_VaValidateKhiTrong()
    {
        using var db = Db.Mo();
        var tk = db.TaiKhoans.AsNoTracking().First(t => t.DaXoa != true);

        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaTaiKhoan(tk.TenTk);
            Rf.Call(f, "FormSuaTaiKhoan_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);
            Assert.Equal(tk.TenTk, Rf.Get<TextBox>(f, "txtTenTK").Text);
            Assert.Equal(tk.MaNv, Rf.Get<TextBox>(f, "txtMaNV").Text);
            Assert.Equal(tk.CapDoQuyen.ToString(), Rf.Get<ComboBox>(f, "cbCapDoQuyen").SelectedItem?.ToString());

            Rf.Get<TextBox>(f, "txtMatKhau").Text = "";
            Rf.Call(f, "btnCapNhat_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Vui lòng điền đầy đủ thông tin!"));
        });
    }

    [DbFact]
    public void SuaTaiKhoan_TenKhongTonTai_BaoKhongTimThay()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormSuaTaiKhoan("khong_ton_tai_xyz");
            Rf.Call(f, "FormSuaTaiKhoan_Load", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Không tìm thấy tài khoản cần cập nhật!"));
        });
    }
}
