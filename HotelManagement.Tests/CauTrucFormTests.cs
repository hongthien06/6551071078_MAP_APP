using HotelManagement.HoaDon;
using HotelManagement.PhieuThue;
using HotelManagement.TaiKhoan;
using Xunit;

namespace HotelManagement.Tests;

/// <summary>
/// Kiểm tra cấu trúc form LAB3: sự kiện được đăng ký đúng 1 lần,
/// tên cột / item khớp với chuỗi mà code xử lý đang so sánh.
/// Không cần CSDL.
/// </summary>
public class CauTrucFormTests
{
    public static IEnumerable<object[]> CacForm() => new[]
    {
        new object[] { typeof(FormThemDichVuPhongDaDung) },
        new object[] { typeof(FormSuaChiTietPhieuThue) },
        new object[] { typeof(FormDanhSachDichVuPhongDaDung) },
        new object[] { typeof(FormChiTietPhieuThue) },
        new object[] { typeof(FormDanhSachPhieuThue) },
        new object[] { typeof(FormHoaDon) },
        new object[] { typeof(FormDanhSachHoaDon) },
        new object[] { typeof(FormDanhSachTaiKhoan) },
        new object[] { typeof(FormThemTaiKhoan) },
        new object[] { typeof(FormSuaTaiKhoan) },
    };

    [Theory]
    [MemberData(nameof(CacForm))]
    public void SuKienLoad_DuocDangKyDungMotLan(Type loaiForm)
    {
        int soLan = -1;
        Sta.Run(() =>
        {
            using var f = (Form)Activator.CreateInstance(loaiForm)!;
            soLan = Rf.CountHandlers(f, "Load");
        });
        // Mỗi form LAB3 đều có hàm Form..._Load nên phải được gọi đúng 1 lần
        Assert.Equal(1, soLan);
    }

    [Theory]
    [MemberData(nameof(CacForm))]
    public void MoiNut_SuKienClick_DuocDangKyDungMotLan(Type loaiForm)
    {
        var loi = new List<string>();
        Sta.Run(() =>
        {
            using var f = (Form)Activator.CreateInstance(loaiForm)!;
            foreach (var btn in Rf.AllControls(f).OfType<Button>())
            {
                int n = Rf.CountHandlers(btn, "Click");
                if (n != 1) loi.Add($"{btn.Name}: {n} handler");
            }
        });
        Assert.True(loi.Count == 0, string.Join("; ", loi));
    }

    [Theory]
    [MemberData(nameof(CacForm))]
    public void LuoiVaOTimKiem_KhongDangKyTrungSuKien(Type loaiForm)
    {
        var loi = new List<string>();
        Sta.Run(() =>
        {
            using var f = (Form)Activator.CreateInstance(loaiForm)!;
            foreach (var c in Rf.AllControls(f))
            {
                if (c is DataGridView g)
                {
                    foreach (var ev in new[] { "CellClick", "CellContentClick" })
                    {
                        int n = Rf.CountHandlers(g, ev);
                        if (n > 1) loi.Add($"{g.Name}.{ev}: {n}");
                    }
                }
                if (c is TextBox t)
                {
                    int n = Rf.CountHandlers(t, "Text");
                    if (n > 1) loi.Add($"{t.Name}.TextChanged: {n}");
                }
            }
        });
        Assert.True(loi.Count == 0, string.Join("; ", loi));
    }

    [Fact]
    public void FormChiTietPhieuThue_CotLuoi_KhopTenDungTrongCellClick()
    {
        var tenCot = new List<string>();
        Sta.Run(() =>
        {
            using var f = new FormChiTietPhieuThue();
            Rf.Call(f, "CauHinhLuoi");
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachCTPT");
            tenCot.AddRange(g.Columns.Cast<DataGridViewColumn>().Select(c => c.Name));
            Assert.Equal(1, Rf.CountHandlers(g, "CellClick"));
        });
        foreach (var ten in new[] { "maCTDP", "MapH", "CheckIn", "CheckOut", "TrangThai", "ThanhTien", "Huy", "Sua", "DichVu", "ThanhToan" })
            Assert.Contains(ten, tenCot);
    }

    [Fact]
    public void FormSuaChiTietPhieuThue_ComboTrangThai_DungGiaTriSoSanh()
    {
        List<string> items = new();
        Sta.Run(() =>
        {
            using var f = new FormSuaChiTietPhieuThue();
            var cb = Rf.Get<ComboBox>(f, "cbTrangThaiCTDP");
            items = cb.Items.Cast<object>().Select(o => o.ToString()!).ToList();
            Assert.Equal(ComboBoxStyle.DropDownList, cb.DropDownStyle);
        });
        Assert.Equal(new[] { "Đang thuê", "Đã thanh toán" }, items);
    }

    [Theory]
    [InlineData(typeof(FormThemTaiKhoan))]
    [InlineData(typeof(FormSuaTaiKhoan))]
    public void FormTaiKhoan_ComboCapDoQuyen_LaSo(Type loaiForm)
    {
        List<string> items = new();
        Sta.Run(() =>
        {
            using var f = (Form)Activator.CreateInstance(loaiForm)!;
            var cb = Rf.Get<ComboBox>(f, "cbCapDoQuyen");
            items = cb.Items.Cast<object>().Select(o => o.ToString()!).ToList();
        });
        // Chỉ có 3 cấp được định nghĩa (1 - Nhân viên, 2 - Quản lý, 3 - Admin)
        Assert.Equal(new[] { "3", "2", "1" }, items);
        Assert.All(items, s => Assert.True(int.TryParse(s, out _)));
    }

    [Fact]
    public void FormDanhSachHoaDon_CotXuatExcel_DeuTonTai()
    {
        var tenCot = new List<string>();
        Sta.Run(() =>
        {
            using var f = new FormDanhSachHoaDon();
            Rf.Call(f, "KhoiTaoBangDuLieu");
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachHoaDon");
            tenCot.AddRange(g.Columns.Cast<DataGridViewColumn>().Select(c => c.Name));
        });
        foreach (var ten in new[] { "MaHD", "NgHD", "TenNV", "MaCTDP", "TriGia", "TrangThai", "XemChiTiet" })
            Assert.Contains(ten, tenCot);
    }

    [Fact]
    public void FormDanhSachTaiKhoan_CotXuatExcel_DeuTonTai()
    {
        var tenCot = new List<string>();
        Sta.Run(() =>
        {
            using var f = new FormDanhSachTaiKhoan();
            Rf.Call(f, "KhoiTaoBangDuLieu");
            var g = Rf.Get<DataGridView>(f, "dtgDanhSachTaiKhoan");
            tenCot.AddRange(g.Columns.Cast<DataGridViewColumn>().Select(c => c.Name));
        });
        foreach (var ten in new[] { "Icon", "TenTK", "TenNV", "CapDoQuyen", "Sua", "Xoa" })
            Assert.Contains(ten, tenCot);
    }

    [Fact]
    public void FormThemDichVu_OSoLuong_ChiNhanChuSo()
    {
        Sta.Run(() =>
        {
            using var f = new FormThemDichVuPhongDaDung();
            var txt = Rf.Get<TextBox>(f, "txtSoLuong");
            Assert.Equal(6, txt.MaxLength);
            Assert.Equal(1, Rf.CountHandlers(txt, "KeyPress"));

            foreach (var (kyTu, biChan) in new[] { ('a', true), ('-', true), ('.', true), ('5', false), ('\b', false) })
            {
                var e = new KeyPressEventArgs(kyTu);
                Rf.Call(f, "txtSoLuong_KeyPress", txt, e);
                Assert.Equal(biChan, e.Handled);
            }
        });
    }
}
