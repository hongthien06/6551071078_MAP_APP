using System.Globalization;
using HotelManagement.HoaDon;
using HotelManagement.PhieuThue;
using HotelManagement.TaiKhoan;
using Xunit;

namespace HotelManagement.Tests;

/// <summary>Test các hàm tiện ích thuần (không cần CSDL).</summary>
public class HamTienIchTests
{
    private static string TaoMa(IEnumerable<string> cacMa, string tienTo = "HD", int soChuSo = 3, int doDaiToiDa = 10)
        => (string)Rf.CallStatic(typeof(FormChiTietPhieuThue), "TaoMaTiepTheo", cacMa, tienTo, soChuSo, doDaiToiDa)!;

    [Fact]
    public void TaoMaTiepTheo_DanhSachRong_TraVeHD001()
    {
        Assert.Equal("HD001", TaoMa(Array.Empty<string>()));
    }

    [Fact]
    public void TaoMaTiepTheo_LayMaLonNhatCongMot_KhongPhuThuocThuTu()
    {
        Assert.Equal("HD006", TaoMa(new[] { "HD001", "HD005", "HD003" }));
    }

    [Fact]
    public void TaoMaTiepTheo_CoKhoangTrong_VanLayLonNhatCongMot()
    {
        // Dữ liệu thật đang thiếu HD016, HD017 -> mã mới vẫn là lớn nhất + 1
        Assert.Equal("HD027", TaoMa(new[] { "HD015", "HD018", "HD026" }));
    }

    [Fact]
    public void TaoMaTiepTheo_KhongPhanBietHoaThuong()
    {
        Assert.Equal("HD010", TaoMa(new[] { "hd009" }));
    }

    [Fact]
    public void TaoMaTiepTheo_BoQuaMaSaiDinhDang()
    {
        Assert.Equal("HD003", TaoMa(new[] { "HDX99", "HD", "PT050", "HD002" }));
    }

    [Fact]
    public void TaoMaTiepTheo_VuotDoDaiToiDa_NemInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TaoMa(new[] { "HD999" }, doDaiToiDa: 5));
        Assert.Contains("HD", ex.Message);
    }

    [Fact]
    public void TaoMaTiepTheo_SauHD999_SinhHD1000()
    {
        // Cột MaHD đã nới lên NVARCHAR(10) -> không còn giới hạn HD999
        Assert.Equal("HD1000", TaoMa(new[] { "HD998", "HD999" }));
        Assert.Equal("HD1001", TaoMa(new[] { "HD999", "HD1000" }));
    }

    [Fact]
    public void ModelEF_MaHD_ChoPhep10KyTu()
    {
        using var db = new HotelManagement.Models.HotelManagementContext();
        var p = db.Model.FindEntityType(typeof(HotelManagement.Models.HoaDon))!.FindProperty("MaHd")!;
        Assert.Equal(10, p.GetMaxLength());
    }

    [Theory]
    [InlineData(1, "Cấp 1 - Nhân viên")]
    [InlineData(2, "Cấp 2 - Quản lý")]
    [InlineData(3, "Cấp 3 - Admin")]
    [InlineData(0, "Cấp 0")]
    public void TenQuyen_TraVeTenHienThiDung(int capDo, string ketQua)
    {
        string? thucTe = null;
        Sta.Run(() =>
        {
            using var f = new FormDanhSachTaiKhoan();
            thucTe = (string)Rf.Call(f, "TenQuyen", capDo)!;
        });
        Assert.Equal(ketQua, thucTe);
    }

    [Fact]
    public void DinhDangTien_DungDinhDangVietNam()
    {
        string? thucTe = null;
        Sta.Run(() =>
        {
            using var f = new FormHoaDon();
            thucTe = (string)Rf.Call(f, "DinhDangTien", 1234567m)!;
        });
        string mongDoi = 1234567m.ToString("N0", new CultureInfo("vi-VN")) + " VND";
        Assert.Equal(mongDoi, thucTe);
        Assert.Equal("1.234.567 VND", thucTe);
    }
}
