# 6551071078_MAP_APP

Võ Hồng Thiên - 6551071078 - CQ.65.CNTT

## Quản lý hoạt động khách sạn (HotelManagement) - LAB 1, LAB 2, LAB 3

Ứng dụng WinForms (.NET 8, C#) + Entity Framework Core + SQL Server.

### LAB 1
- CSDL `HotelManagement` (`Database/Database.sql`) và scaffold EF Core vào thư mục `Models`.
- Resources (icon/hình ảnh) trong `Resources/` + `Properties/Resources.resx`.
- Đăng nhập (`DangNhap/`), form chính + trang chủ (`FormMain`, `FormTrangChu`).
- Sơ đồ phòng với bộ lọc (`SoDoPhong/`).
- Quản lý phòng: danh sách, tìm kiếm, xuất Excel, thêm / sửa / xóa mềm (`Phong/`).
- Quản lý loại phòng: danh sách, tìm kiếm, xuất Excel, thêm / sửa (`LoaiPhong/`).

### LAB 2
- Quản lý dịch vụ (`DichVu/`), tiện nghi (`TienNghi/`), chi tiết tiện nghi theo loại phòng (`ChiTietTienNghi/`).
- Quản lý khách hàng (`KhachHang/`), nhân viên (`NhanVien/`).
- Đặt phòng (`SoDoPhong/FormDatPhong`).

### LAB 3
1. Quản lý dịch vụ khách dùng khi đang thuê phòng (`PhieuThue/FormThemDichVuPhongDaDung`, `FormDanhSachDichVuPhongDaDung`).
2. Quản lý phiếu đặt/thuê phòng: danh sách, tìm kiếm, xuất Excel, chi tiết phiếu, huỷ, cập nhật trạng thái, thanh toán (`PhieuThue/`). Menu "Danh sách đặt phòng".
3. Quản lý hoá đơn: danh sách, lọc theo ngày, tìm kiếm, xuất Excel, xem và in hoá đơn PDF (`HoaDon/`). Menu "Danh sách hóa đơn".
4. Quản lý tài khoản và quyền: danh sách, tìm kiếm, xuất Excel, thêm / sửa / xoá (`TaiKhoan/`). Menu "Danh sách tài khoản".
5. Dashboard thống kê kinh doanh theo khoảng thời gian (`ThongKe/FormThongKe`, biểu đồ `WinForms.DataVisualization`). Menu "Thống kê".

### Chạy thử
1. Chạy `Database/Database.sql` trong SQL Server (SSMS), hoặc `sqlcmd -I -i Database/Database.sql` (cần cờ `-I`).
   CSDL đã tạo từ bản cũ thì chạy thêm `Database/NangCap_MaHD_10KyTu.sql` (mã hoá đơn dài 10 ký tự).
2. Đổi `ConnectionString` trong `Models/HotelManagementContext.custom.cs` cho đúng máy (mặc định `(localdb)\MSSQLLocalDB`).
3. Mở `HotelManagement.slnx` bằng Visual Studio 2022, chọn project `HotelManagement` làm Startup Project và chạy. Tài khoản mẫu: `admin` / `1234`.

**Ghi chú về `Database.sql`:** bản trong repo thêm cột `DonGia` cho bảng `CTDV` (file gốc thiếu cột này nên lỗi ở trigger `CapNhatGiaDV` và các lệnh INSERT CTDV).
