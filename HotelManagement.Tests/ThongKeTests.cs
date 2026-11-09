using System.Windows.Forms.DataVisualization.Charting;
using HotelManagement.ThongKe;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelManagement.Tests;

/// <summary>Test dashboard thống kê (mục 5) — chỉ đọc CSDL.</summary>
public class ThongKeTests
{
    [DbFact]
    public void TinhThongKe_ToanBoKhoangThoiGian_KhopTruyVanDocLap()
    {
        using var db = Db.Mo();
        DateTime tu = new(2000, 1, 1), den = new(2079, 1, 1);
        // Ngày ngoài phạm vi smalldatetime (1900–2079) không được làm crash
        var kq = FormThongKe.TinhThongKe(db, new DateTime(1800, 1, 1), new DateTime(2100, 1, 1));

        var hd = db.HoaDons.AsNoTracking().Include(h => h.MaCtdpNavigation)
            .Where(h => h.DaXoa != true && h.NgHd >= tu && h.NgHd < den.AddDays(1)).ToList();
        decimal tienPhong = hd.Sum(h => h.MaCtdpNavigation.ThanhTien ?? 0);
        var maCtdp = hd.Select(h => h.MaCtdp).ToHashSet();
        decimal tienDv = db.Ctdvs.AsNoTracking().Include(v => v.MaDvNavigation)
            .Where(v => v.DaXoa != true).ToList()
            .Where(v => maCtdp.Contains(v.MaCtdp))
            .Sum(v => v.ThanhTien ?? v.Sl * v.MaDvNavigation.DonGia);
        int soDat = db.Ctdps.Count(c => c.DaXoa != true && c.CheckIn >= tu && c.CheckIn < den.AddDays(1));

        Assert.Equal(tienPhong, kq.DoanhThuPhong);
        Assert.Equal(tienDv, kq.DoanhThuDichVu);
        Assert.Equal(soDat, kq.SoPhongDat);
        Assert.Equal(kq.DoanhThuPhong, kq.DoanhThuTheoLoaiPhong.Sum(d => d.GiaTri));
        Assert.Equal(kq.SoPhongDat, (int)kq.SoLuotDatTheoLoaiPhong.Sum(d => d.GiaTri));
        Assert.Equal(kq.DoanhThuDichVu, kq.DoanhThuTheoDichVu.Sum(d => d.GiaTri));
        // Danh sách xếp giảm dần để phần tử đầu là "cao nhất"
        Assert.Equal(kq.DoanhThuTheoLoaiPhong.OrderByDescending(d => d.GiaTri).Select(d => d.GiaTri), kq.DoanhThuTheoLoaiPhong.Select(d => d.GiaTri));
    }

    [DbFact]
    public void TinhThongKe_KhoangKhongCoDuLieu_TraVeRong()
    {
        using var db = Db.Mo();
        var kq = FormThongKe.TinhThongKe(db, new DateTime(1990, 1, 1), new DateTime(1990, 1, 2));
        Assert.Equal(0, kq.DoanhThuPhong);
        Assert.Equal(0, kq.DoanhThuDichVu);
        Assert.Equal(0, kq.SoPhongDat);
        Assert.Empty(kq.DoanhThuTheoLoaiPhong);
        Assert.Empty(kq.SoLuongTheoDichVu);
    }

    [DbFact]
    public void TinhThongKe_TinhCaNgayCuoi()
    {
        using var db = Db.Mo();
        var hd = db.HoaDons.AsNoTracking().Include(h => h.MaCtdpNavigation)
            .Where(h => h.DaXoa != true && h.NgHd != null).OrderBy(h => h.MaHd).First();
        DateTime ngay = hd.NgHd!.Value.Date;
        // Từ ngày = đến ngày = ngày lập hóa đơn -> hóa đơn đó phải được tính
        var kq = FormThongKe.TinhThongKe(db, ngay, ngay);
        Assert.True(kq.DoanhThuPhong >= (hd.MaCtdpNavigation.ThanhTien ?? 0));
    }

    [DbFact]
    public void FormThongKe_Load_HienSoLieuVaBieuDo()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThongKe();
            Rf.Call(f, "FormThongKe_Load", f, EventArgs.Empty);
            Assert.Empty(w.Messages);

            Assert.EndsWith(" VND", Rf.Get<Label>(f, "lbDoanhThuPhong").Text);
            Assert.EndsWith(" VND", Rf.Get<Label>(f, "lbDoanhThuDV").Text);
            Assert.True(int.Parse(Rf.Get<Label>(f, "lbSoPhongDat").Text) >= 0);
            Assert.NotEqual("-", Rf.Get<Label>(f, "lbLoaiPhongDTCaoNhat").Text);

            foreach (var ten in new[] { "chartDoanhThuPhong", "chartSoPhongDat" })
            {
                var chart = Rf.Get<Chart>(f, ten);
                var s = chart.Series[0];
                Assert.Equal(SeriesChartType.Column, s.ChartType);
                Assert.True(s.Points.Count > 0);
                // Mỗi loại phòng phải là một cột riêng (không chồng lên nhau)
                Assert.True(s.IsXValueIndexed, ten + ": các cột đang chồng lên cùng vị trí X");
                Assert.Equal(s.Points.Count, s.Points.Select(p => p.AxisLabel).Distinct().Count());
            }
            Assert.Equal(SeriesChartType.Pie, Rf.Get<Chart>(f, "chartDichVu").Series[0].ChartType);
        });
    }

    [Fact]
    public void FormThongKe_TuNgayLonHonDenNgay_BaoLoi()
    {
        Sta.Run(() =>
        {
            using var w = new MessageBoxWatcher();
            using var f = new FormThongKe();
            Rf.Get<DateTimePicker>(f, "dtpTuNgay").Value = new DateTime(2025, 5, 18);
            Rf.Get<DateTimePicker>(f, "dtpDenNgay").Value = new DateTime(2025, 5, 1);
            Rf.Call(f, "btnXemThongKe_Click", f, EventArgs.Empty);
            Assert.Contains(w.Messages, m => m.Contains("Từ ngày không được lớn hơn đến ngày!"));
        });
    }

    [Fact]
    public void FormThongKe_SuKienDangKyMotLan()
    {
        Sta.Run(() =>
        {
            using var f = new FormThongKe();
            Assert.Equal(1, Rf.CountHandlers(f, "Load"));
            Assert.Equal(1, Rf.CountHandlers(Rf.Get<Button>(f, "btnXemThongKe"), "Click"));
        });
    }
}
