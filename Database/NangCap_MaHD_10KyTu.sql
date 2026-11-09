-- Nâng cấp CSDL đã tạo từ Database.sql cũ: cột HoaDon.MaHD từ NVARCHAR(5) lên NVARCHAR(10)
-- để mã hóa đơn không bị giới hạn ở HD999. Chạy được nhiều lần (đã đổi rồi thì bỏ qua).
USE HotelManagement
GO
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('HoaDon') AND name = 'MaHD' AND max_length < 20)
BEGIN
    DECLARE @pk SYSNAME = (SELECT name FROM sys.key_constraints
                           WHERE parent_object_id = OBJECT_ID('HoaDon') AND type = 'PK')
    EXEC('ALTER TABLE HoaDon DROP CONSTRAINT ' + @pk)
    ALTER TABLE HoaDon ALTER COLUMN MaHD NVARCHAR(10) NOT NULL
    EXEC('ALTER TABLE HoaDon ADD CONSTRAINT ' + @pk + ' PRIMARY KEY (MaHD)')
END
GO
ALTER TRIGGER CapNhatGiaTriHoaDon ON HoaDon FOR INSERT,UPDATE
AS
BEGIN
	DECLARE @MaHD NVARCHAR(10), @MaCTDP NVARCHAR(7), @TongTienHD MONEY, @TongTienDV MONEY, @TongTienPhong MONEY
	SET @MaHD = (SELECT MaHD FROM inserted)
	SET @MaCTDP = (SELECT MaCTDP FROM inserted)
	SET @TongTienDV = 0
	SET @TongTienDV = (SELECT SUM(ThanhTien) FROM CTDV WHERE MaCTDP = @MaCTDP GROUP BY MaCTDP)
	SET @TongTienPhong = (SELECT ThanhTien FROM CTDP WHERE MaCTDP=@MaCTDP)
	IF ( NOT EXISTS( SELECT * FROM CTDV WHERE MaCTDP = @MaCTDP))
	BEGIN
		SET @TongTienDV = 0
	END
	UPDATE HoaDon
	SET TriGia = @TongTienDV+@TongTienPhong
	WHERE MaHD=@MaHD
END
GO
