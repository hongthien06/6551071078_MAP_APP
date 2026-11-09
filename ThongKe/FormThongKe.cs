using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using HotelManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.ThongKe
{
    public partial class FormThongKe : Form
    {
        public sealed class DongThongKe
        {
            public string Ten { get; set; } = "";
            public decimal GiaTri { get; set; }
        }

        public sealed class KetQuaThongKe
        {
            public decimal DoanhThuPhong { get; set; }
            public decimal DoanhThuDichVu { get; set; }
            public int SoPhongDat { get; set; }
            public List<DongThongKe> DoanhThuTheoLoaiPhong { get; set; } = new();
            public List<DongThongKe> SoLuotDatTheoLoaiPhong { get; set; } = new();
            public List<DongThongKe> DoanhThuTheoDichVu { get; set; } = new();
            public List<DongThongKe> SoLuongTheoDichVu { get; set; } = new();
        }

        private static readonly DateTime NgayNhoNhat = new DateTime(1900, 1, 1);
        private static readonly DateTime NgayLonNhat = new DateTime(2079, 6, 6);

        private readonly CultureInfo viVN = new CultureInfo("vi-VN");

        public FormThongKe()
        {
            InitializeComponent();

            dtpTuNgay.MinDate = NgayNhoNhat;
            dtpTuNgay.MaxDate = NgayLonNhat;
            dtpDenNgay.MinDate = NgayNhoNhat;
            dtpDenNgay.MaxDate = NgayLonNhat;
        }

        private static DateTime GioiHanNgay(DateTime ngay)
        {
            if (ngay < NgayNhoNhat) return NgayNhoNhat;
            if (ngay > NgayLonNhat) return NgayLonNhat;
            return ngay;
        }

        private void FormThongKe_Load(object sender, EventArgs e)
        {
            dtpTuNgay.Format = DateTimePickerFormat.Long;
            dtpDenNgay.Format = DateTimePickerFormat.Long;

            DateTime tuNgay = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            try
            {
                using var db = new HotelManagementContext();
                DateTime? ngayDauTien = db.HoaDons
                    .Where(h => h.DaXoa != true)
                    .Min(h => h.NgHd);
                if (ngayDauTien.HasValue)
                    tuNgay = ngayDauTien.Value.Date;
            }
            catch
            {
                // Không lấy được ngày đầu tiên thì dùng ngày đầu tháng hiện tại
            }

            dtpTuNgay.Value = tuNgay;
            dtpDenNgay.Value = DateTime.Today;

            XemThongKe();
        }

        private void btnXemThongKe_Click(object sender, EventArgs e)
        {
            XemThongKe();
        }

        private string DinhDangTien(decimal soTien)
        {
            return soTien.ToString("N0", viVN) + " VND";
        }

        /// <summary>
        /// Tổng hợp số liệu kinh doanh trong khoảng [tuNgay, denNgay] (tính theo ngày).
        /// - Doanh thu thuê phòng / dịch vụ: theo hóa đơn có ngày lập trong khoảng.
        /// - Số phòng đặt: số chi tiết đặt phòng có ngày nhận phòng trong khoảng.
        /// </summary>
        public static KetQuaThongKe TinhThongKe(HotelManagementContext db, DateTime tuNgay, DateTime denNgay)
        {
            // Cột CheckIn kiểu smalldatetime chỉ nhận từ 01/01/1900 đến 06/06/2079
            DateTime batDau = GioiHanNgay(tuNgay.Date);
            DateTime ketThuc = GioiHanNgay(denNgay.Date.AddDays(1));

            var hoaDon = db.HoaDons
                .AsNoTracking()
                .Where(h => h.DaXoa != true && h.NgHd >= batDau && h.NgHd < ketThuc)
                .Select(h => new
                {
                    h.MaCtdp,
                    TienPhong = h.MaCtdpNavigation.ThanhTien ?? 0,
                    TenLph = h.MaCtdpNavigation.MaPhNavigation.MaLphNavigation.TenLph
                })
                .ToList();

            List<string> dsMaCtdp = hoaDon.Select(h => h.MaCtdp).Distinct().ToList();

            var dichVu = db.Ctdvs
                .AsNoTracking()
                .Where(v => v.DaXoa != true && dsMaCtdp.Contains(v.MaCtdp))
                .Select(v => new
                {
                    TenDv = v.MaDvNavigation.TenDv,
                    v.Sl,
                    ThanhTien = v.ThanhTien ?? (v.Sl * v.MaDvNavigation.DonGia)
                })
                .ToList();

            var datPhong = db.Ctdps
                .AsNoTracking()
                .Where(c => c.DaXoa != true && c.CheckIn >= batDau && c.CheckIn < ketThuc)
                .Select(c => c.MaPhNavigation.MaLphNavigation.TenLph)
                .ToList();

            return new KetQuaThongKe
            {
                DoanhThuPhong = hoaDon.Sum(h => h.TienPhong),
                DoanhThuDichVu = dichVu.Sum(d => d.ThanhTien),
                SoPhongDat = datPhong.Count,
                DoanhThuTheoLoaiPhong = hoaDon
                    .GroupBy(h => h.TenLph)
                    .Select(g => new DongThongKe { Ten = g.Key, GiaTri = g.Sum(x => x.TienPhong) })
                    .OrderByDescending(d => d.GiaTri).ThenBy(d => d.Ten)
                    .ToList(),
                SoLuotDatTheoLoaiPhong = datPhong
                    .GroupBy(t => t)
                    .Select(g => new DongThongKe { Ten = g.Key, GiaTri = g.Count() })
                    .OrderByDescending(d => d.GiaTri).ThenBy(d => d.Ten)
                    .ToList(),
                DoanhThuTheoDichVu = dichVu
                    .GroupBy(d => d.TenDv)
                    .Select(g => new DongThongKe { Ten = g.Key, GiaTri = g.Sum(x => x.ThanhTien) })
                    .OrderByDescending(d => d.GiaTri).ThenBy(d => d.Ten)
                    .ToList(),
                SoLuongTheoDichVu = dichVu
                    .GroupBy(d => d.TenDv)
                    .Select(g => new DongThongKe { Ten = g.Key, GiaTri = g.Sum(x => x.Sl) })
                    .OrderByDescending(d => d.GiaTri).ThenBy(d => d.Ten)
                    .ToList()
            };
        }

        private void XemThongKe()
        {
            if (dtpTuNgay.Value.Date > dtpDenNgay.Value.Date)
            {
                MessageBox.Show("Từ ngày không được lớn hơn đến ngày!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var db = new HotelManagementContext();
                KetQuaThongKe kq = TinhThongKe(db, dtpTuNgay.Value, dtpDenNgay.Value);
                HienThiKetQua(kq);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lập báo cáo thống kê: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HienThiKetQua(KetQuaThongKe kq)
        {
            lbDoanhThuPhong.Text = DinhDangTien(kq.DoanhThuPhong);
            lbDoanhThuDV.Text = DinhDangTien(kq.DoanhThuDichVu);
            lbSoPhongDat.Text = kq.SoPhongDat.ToString();

            var top1 = kq.DoanhThuTheoLoaiPhong.FirstOrDefault();
            lbLoaiPhongDTCaoNhat.Text = top1 == null ? "Không có dữ liệu" : $"{top1.Ten} ({DinhDangTien(top1.GiaTri)})";

            var top2 = kq.SoLuotDatTheoLoaiPhong.FirstOrDefault();
            lbLoaiPhongDatNhieuNhat.Text = top2 == null ? "Không có dữ liệu" : $"{top2.Ten} ({top2.GiaTri:N0} lượt)";

            var top3 = kq.DoanhThuTheoDichVu.FirstOrDefault();
            lbDichVuDTCaoNhat.Text = top3 == null ? "Không có dữ liệu" : $"{top3.Ten} ({DinhDangTien(top3.GiaTri)})";

            VeBieuDoCot(chartDoanhThuPhong, kq.DoanhThuTheoLoaiPhong, "#,##0");
            VeBieuDoCot(chartSoPhongDat, kq.SoLuotDatTheoLoaiPhong, "0");

            Series tron = chartDichVu.Series[0];
            tron.Points.Clear();
            foreach (var d in kq.SoLuongTheoDichVu)
                tron.Points.AddXY(d.Ten, d.GiaTri);
        }

        private static void VeBieuDoCot(Chart chart, List<DongThongKe> duLieu, string dinhDangTrucY)
        {
            Series cot = chart.Series[0];
            cot.ChartType = SeriesChartType.Column;
            // Trục X là tên loại phòng -> đánh chỉ số để các cột không chồng lên nhau
            cot.IsXValueIndexed = true;
            cot.Points.Clear();
            foreach (var d in duLieu)
                cot.Points.AddXY(d.Ten, d.GiaTri);

            ChartArea vung = chart.ChartAreas[0];
            vung.AxisX.Interval = 1;
            vung.AxisX.MajorGrid.Enabled = false;
            vung.AxisY.LabelStyle.Format = dinhDangTrucY;
        }
    }
}
