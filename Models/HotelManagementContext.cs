using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Models;

public partial class HotelManagementContext : DbContext
{
    public HotelManagementContext(DbContextOptions<HotelManagementContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ctdp> Ctdps { get; set; }

    public virtual DbSet<Ctdv> Ctdvs { get; set; }

    public virtual DbSet<Cttn> Cttns { get; set; }

    public virtual DbSet<DichVu> DichVus { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }

    public virtual DbSet<NhanVien> NhanViens { get; set; }

    public virtual DbSet<PhieuThue> PhieuThues { get; set; }

    public virtual DbSet<Phong> Phongs { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TienNghi> TienNghis { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ctdp>(entity =>
        {
            entity.HasKey(e => e.MaCtdp).HasName("PK__CTDP__1E4E40F807A47386");

            entity.ToTable("CTDP", tb => tb.HasTrigger("CapNhatGiaCTDP"));

            entity.Property(e => e.MaCtdp)
                .HasMaxLength(7)
                .HasColumnName("MaCTDP");
            entity.Property(e => e.CheckIn).HasColumnType("smalldatetime");
            entity.Property(e => e.CheckOut).HasColumnType("smalldatetime");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.DonGia).HasColumnType("money");
            entity.Property(e => e.MaPh)
                .HasMaxLength(5)
                .HasColumnName("MaPH");
            entity.Property(e => e.MaPt)
                .HasMaxLength(5)
                .HasColumnName("MaPT");
            entity.Property(e => e.ThanhTien)
                .HasDefaultValue(0m)
                .HasColumnType("money");
            entity.Property(e => e.TheoGio).HasDefaultValue(false);
            entity.Property(e => e.TrangThai).HasMaxLength(20);

            entity.HasOne(d => d.MaPhNavigation).WithMany(p => p.Ctdps)
                .HasForeignKey(d => d.MaPh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CTDP_MaPH_Forein");

            entity.HasOne(d => d.MaPtNavigation).WithMany(p => p.Ctdps)
                .HasForeignKey(d => d.MaPt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CTDP_MaPT_Forein");
        });

        modelBuilder.Entity<Ctdv>(entity =>
        {
            entity.HasKey(e => new { e.MaCtdp, e.MaDv });

            entity.ToTable("CTDV", tb => tb.HasTrigger("CapNhatGiaDV"));

            entity.Property(e => e.MaCtdp)
                .HasMaxLength(7)
                .HasColumnName("MaCTDP");
            entity.Property(e => e.MaDv)
                .HasMaxLength(5)
                .HasColumnName("MaDV");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.DonGia).HasColumnType("money");
            entity.Property(e => e.Sl).HasColumnName("SL");
            entity.Property(e => e.ThanhTien)
                .HasDefaultValue(0m)
                .HasColumnType("money");

            entity.HasOne(d => d.MaCtdpNavigation).WithMany(p => p.Ctdvs)
                .HasForeignKey(d => d.MaCtdp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CTDV_MaCTDP_foreign");

            entity.HasOne(d => d.MaDvNavigation).WithMany(p => p.Ctdvs)
                .HasForeignKey(d => d.MaDv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CTDV_MaDV_foreign");
        });

        modelBuilder.Entity<Cttn>(entity =>
        {
            entity.HasKey(e => new { e.MaLph, e.MaTn });

            entity.ToTable("CTTN");

            entity.Property(e => e.MaLph)
                .HasMaxLength(5)
                .HasColumnName("MaLPH");
            entity.Property(e => e.MaTn)
                .HasMaxLength(5)
                .HasColumnName("MaTN");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.Sl)
                .HasDefaultValue(-1)
                .HasColumnName("SL");

            entity.HasOne(d => d.MaLphNavigation).WithMany(p => p.Cttns)
                .HasForeignKey(d => d.MaLph)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CTTN_MaLPH_foreign");

            entity.HasOne(d => d.MaTnNavigation).WithMany(p => p.Cttns)
                .HasForeignKey(d => d.MaTn)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("CTTN_MaTN_foreign");
        });

        modelBuilder.Entity<DichVu>(entity =>
        {
            entity.HasKey(e => e.MaDv).HasName("PK__DichVu__2725865783ACBA50");

            entity.ToTable("DichVu");

            entity.Property(e => e.MaDv)
                .HasMaxLength(5)
                .HasColumnName("MaDV");
            entity.Property(e => e.DonGia).HasColumnType("money");
            entity.Property(e => e.LoaiDv)
                .HasMaxLength(20)
                .HasColumnName("LoaiDV");
            entity.Property(e => e.SlconLai)
                .HasDefaultValue(-1)
                .HasColumnName("SLConLai");
            entity.Property(e => e.TenDv)
                .HasMaxLength(20)
                .HasColumnName("TenDV");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.MaHd).HasName("PK__HoaDon__2725A6E09CCC66B1");

            entity.ToTable("HoaDon", tb => tb.HasTrigger("CapNhatGiaTriHoaDon"));

            entity.Property(e => e.MaHd)
                .HasMaxLength(5)
                .HasColumnName("MaHD");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.MaCtdp)
                .HasMaxLength(7)
                .HasColumnName("MaCTDP");
            entity.Property(e => e.MaNv)
                .HasMaxLength(5)
                .HasColumnName("MaNV");
            entity.Property(e => e.NgHd)
                .HasColumnType("smalldatetime")
                .HasColumnName("NgHD");
            entity.Property(e => e.TrangThai).HasMaxLength(20);
            entity.Property(e => e.TriGia)
                .HasDefaultValue(0m)
                .HasColumnType("money");

            entity.HasOne(d => d.MaCtdpNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaCtdp)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("hoadon_MaCTDP_foreign");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.MaNv)
                .HasConstraintName("hoadon_manv_foreign");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.MaKh).HasName("PK__KhachHan__2725CF1E04BDC93A");

            entity.ToTable("KhachHang");

            entity.HasIndex(e => e.CccdPassport, "UQ__KhachHan__F7905DCDB9A4B019").IsUnique();

            entity.Property(e => e.MaKh)
                .HasMaxLength(5)
                .HasColumnName("MaKH");
            entity.Property(e => e.CccdPassport)
                .HasMaxLength(12)
                .HasColumnName("CCCD/Passport");
            entity.Property(e => e.GioiTinh).HasMaxLength(3);
            entity.Property(e => e.QuocTich).HasMaxLength(30);
            entity.Property(e => e.Sdt)
                .HasMaxLength(10)
                .HasColumnName("SDT");
            entity.Property(e => e.TenKh)
                .HasMaxLength(50)
                .HasColumnName("TenKH");
        });

        modelBuilder.Entity<LoaiPhong>(entity =>
        {
            entity.HasKey(e => e.MaLph).HasName("PK__LoaiPhon__3B9BD79220F08D1F");

            entity.ToTable("LoaiPhong");

            entity.Property(e => e.MaLph)
                .HasMaxLength(5)
                .HasColumnName("MaLPH");
            entity.Property(e => e.GiaGio).HasColumnType("money");
            entity.Property(e => e.GiaNgay).HasColumnType("money");
            entity.Property(e => e.TenLph)
                .HasMaxLength(20)
                .HasColumnName("TenLPH");
        });

        modelBuilder.Entity<NhanVien>(entity =>
        {
            entity.HasKey(e => e.MaNv).HasName("PK__NhanVien__2725D70AFEEE9353");

            entity.ToTable("NhanVien");

            entity.HasIndex(e => e.Cccd, "UQ__NhanVien__A955A0AA84E4293A").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__NhanVien__A9D10534FFE25F57").IsUnique();

            entity.HasIndex(e => e.Sdt, "UQ__NhanVien__CA1930A51B8215B6").IsUnique();

            entity.Property(e => e.MaNv)
                .HasMaxLength(5)
                .HasColumnName("MaNV");
            entity.Property(e => e.Cccd)
                .HasMaxLength(12)
                .HasColumnName("CCCD");
            entity.Property(e => e.ChucVu).HasMaxLength(50);
            entity.Property(e => e.DiaChi).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.GioiTinh).HasMaxLength(3);
            entity.Property(e => e.Luong).HasColumnType("money");
            entity.Property(e => e.Sdt)
                .HasMaxLength(10)
                .HasColumnName("SDT");
            entity.Property(e => e.TenNv)
                .HasMaxLength(50)
                .HasColumnName("TenNV");
        });

        modelBuilder.Entity<PhieuThue>(entity =>
        {
            entity.HasKey(e => e.MaPt).HasName("PK__PhieuThu__2725E7F610EA04A5");

            entity.ToTable("PhieuThue");

            entity.Property(e => e.MaPt)
                .HasMaxLength(5)
                .HasColumnName("MaPT");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.MaKh)
                .HasMaxLength(5)
                .HasColumnName("MaKH");
            entity.Property(e => e.MaNv)
                .HasMaxLength(5)
                .HasColumnName("MaNV");
            entity.Property(e => e.NgPt)
                .HasColumnType("smalldatetime")
                .HasColumnName("NgPT");

            entity.HasOne(d => d.MaKhNavigation).WithMany(p => p.PhieuThues)
                .HasForeignKey(d => d.MaKh)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("phieuthue_makh_foreign");

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.PhieuThues)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("phieuthue_manv_foreign");
        });

        modelBuilder.Entity<Phong>(entity =>
        {
            entity.HasKey(e => e.MaPh).HasName("PK__Phong__2725E7FA6372D013");

            entity.ToTable("Phong");

            entity.Property(e => e.MaPh)
                .HasMaxLength(5)
                .HasColumnName("MaPH");
            entity.Property(e => e.GhiChu).HasMaxLength(100);
            entity.Property(e => e.MaLph)
                .HasMaxLength(5)
                .HasColumnName("MaLPH");
            entity.Property(e => e.Ttdd)
                .HasMaxLength(20)
                .HasColumnName("TTDD");
            entity.Property(e => e.Ttph)
                .HasMaxLength(20)
                .HasColumnName("TTPH");

            entity.HasOne(d => d.MaLphNavigation).WithMany(p => p.Phongs)
                .HasForeignKey(d => d.MaLph)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("phong_malph_foreign");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.TenTk).HasName("PK__TaiKhoan__4CF9E776BD70C772");

            entity.ToTable("TaiKhoan");

            entity.Property(e => e.TenTk)
                .HasMaxLength(50)
                .HasColumnName("TenTK");
            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.MaNv)
                .HasMaxLength(5)
                .HasColumnName("MaNV");
            entity.Property(e => e.Password).HasMaxLength(1000);

            entity.HasOne(d => d.MaNvNavigation).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.MaNv)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("TaiKhoan_manv_foreign");
        });

        modelBuilder.Entity<TienNghi>(entity =>
        {
            entity.HasKey(e => e.MaTn).HasName("PK__TienNghi__2725007354E6B77D");

            entity.ToTable("TienNghi");

            entity.Property(e => e.MaTn)
                .HasMaxLength(5)
                .HasColumnName("MaTN");
            entity.Property(e => e.TenTn)
                .HasMaxLength(50)
                .HasColumnName("TenTN");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
