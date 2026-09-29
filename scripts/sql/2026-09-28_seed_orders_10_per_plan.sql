
-- ============================================================================
-- SEED SCRIPT: Bổ sung đơn hàng (Orders) cho tất cả 17 gói cước khi đăng ký
-- Mỗi gói cước có chính xác 10 bản ghi đơn hàng (Tổng cộng 170 đơn hàng)
-- ============================================================================

USE NexusSystem;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '1. Thêm khách hàng mẫu vào dbo.Customer...';

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0008')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0008', N'Nguyễn Hoàng Nam', 'Individual', 'nam.nguyen88@gmail.com', '+84 903 112 445', N'124 Phố Huế, Hai Bà Trưng, Hà Nội',
        'National ID Card', '001088019234', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0009')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0009', N'Trần Thị Mai Phương', 'Individual', 'phuong.tran.mai@outlook.com', '+84 912 445 667', N'45 Lê Duẩn, Bến Nghé, Quận 1, TP.HCM',
        'National ID Card', '079194002819', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0010')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0010', N'Lê Quang Huy', 'Individual', 'huy.lequang@fpt.edu.vn', '+84 988 776 554', N'78 Cầu Giấy, Quan Hoa, Cầu Giấy, Hà Nội',
        'National ID Card', '001092003817', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0011')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0011', N'Vũ Thu Trang', 'Individual', 'trang.vuthu@gmail.com', '+84 934 556 789', N'210 Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP.HCM',
        'Passport', 'C9921820', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0012')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0012', N'Đặng Quốc Tuấn', 'Individual', 'tuan.dangquoc@yahoo.com', '+84 976 123 456', N'15 Trần Phú, Hải Châu 1, Hải Châu, Đà Nẵng',
        'National ID Card', '048085001928', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0013')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0013', N'Bùi Ngọc Lan', 'Individual', 'ngoclan.bui@gmail.com', '+84 945 678 901', N'88 Lạch Tray, Ngô Quyền, Hải Phòng',
        'Driver''s License', 'DL-HP-99218', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0014')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0014', N'Hoàng Văn Phong', 'Individual', 'phong.hoangvan@techvn.io', '+84 908 998 877', N'32 Võ Văn Tần, Phường 6, Quận 3, TP.HCM',
        'National ID Card', '079090001234', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0015')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0015', N'Đỗ Hải Yến', 'Individual', 'haiyen.do@vnu.edu.vn', '+84 918 223 344', N'144 Xuân Thủy, Dịch Vọng Hậu, Cầu Giấy, Hà Nội',
        'Passport', 'B8871920', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0016')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0016', N'Công ty CP Công Nghệ Sao Việt', 'Corporate', 'contact@saoviet-tech.vn', '+84 28 3822 9988', N'Tòa nhà Bitexco, 2 Hải Triều, Bến Nghé, Quận 1, TP.HCM',
        'Driver''s License', 'DL-CORP-0192', '0312998811', 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0017')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0017', N'Tập đoàn Logistics Á Châu', 'Corporate', 'procurement@achau-logistics.com', '+84 24 3799 1122', N'Tầng 12 Keangnam Landmark 72, Phạm Hùng, Nam Từ Liêm, Hà Nội',
        'Passport', 'P-VN-99281', '0105882910', 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0018')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0018', N'Marcus Vance', 'Individual', 'marcus.vance@vancemedia.com', '+1 (555) 301-4491', N'350 5th Ave, Fl 42, New York, NY 10118',
        'Passport', 'P-US-991203', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0019')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0019', N'Emily Watson', 'Individual', 'emily.watson@designlab.org', '+1 (555) 772-9910', N'224 W 30th St, Chelsea, New York, NY 10001',
        'Driver''s License', 'DL-NY-88271', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0020')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0020', N'Michael Chang', 'Individual', 'm.chang@apexquant.com', '+1 (555) 819-0022', N'100 Wall St, Fl 15, New York, NY 10005',
        'National ID Card', 'ID-US-77192', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0021')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0021', N'Sophia Rossi', 'Individual', 'sophia.rossi@milanocoffee.us', '+1 (555) 991-2345', N'182 Mulberry St, Little Italy, New York, NY 10012',
        'Driver''s License', 'DL-NY-33910', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0022')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0022', N'Alexander Wright', 'Individual', 'a.wright@architectsny.com', '+1 (555) 440-8811', N'550 W 34th St, Hudson Yards, NY 10001',
        'Passport', 'P-US-440192', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0023')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0023', N'Chloe Bennett', 'Individual', 'chloe.bennett@brooklyngallery.art', '+1 (555) 234-9090', N'45 Main St, Dumbo, Brooklyn, NY 11201',
        'National ID Card', 'ID-US-55190', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0024')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0024', N'Daniel Miller', 'Individual', 'd.miller@financestudio.com', '+1 (555) 662-7711', N'120 Broadway, Manhattan, NY 10271',
        'Driver''s License', 'DL-NY-66271', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0025')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0025', N'Olivia Davis', 'Individual', 'olivia.davis@columbia.edu', '+1 (555) 883-2211', N'116th and Broadway, Morningside, NY 10027',
        'National ID Card', 'ID-US-88322', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0026')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0026', N'Lucas Martin', 'Individual', 'lucas.martin@astoriabistro.com', '+1 (555) 331-8899', N'30-10 Broadway, Astoria, Queens, NY 11106',
        'Driver''s License', 'DL-NY-33188', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0027')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0027', N'Isabella Garcia', 'Individual', 'isabella.g@flushingmed.org', '+1 (555) 449-0123', N'136-20 38th Ave, Flushing, Queens, NY 11354',
        'Passport', 'P-US-449012', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0028')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0028', N'Nexus Cloud Innovations LLC', 'Corporate', 'ops@nexuscloud.io', '+1 (555) 900-8800', N'1 World Trade Center, Fl 68, NY 10007',
        'Driver''s License', 'DL-CORP-9008', 'TAX-US-99182', 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0029')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0029', N'Gotham Creative Media Corp', 'Corporate', 'admin@gothamcreative.tv', '+1 (555) 551-0987', N'500 7th Ave, Fl 10, New York, NY 10018',
        'Driver''s License', 'DL-CORP-5510', 'TAX-US-55109', 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0030')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0030', N'Lê Văn Thắng', 'Individual', 'thang.le@vinaphone.com.vn', '+84 913 889 900', N'57 Huỳnh Thúc Kháng, Đống Đa, Hà Nội',
        'National ID Card', '001083002918', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0031')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0031', N'Phạm Minh Tuấn', 'Individual', 'tuan.pm@saigontech.com', '+84 909 334 455', N'12 Quang Trung, Phường 10, Gò Vấp, TP.HCM',
        'National ID Card', '079089001928', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0032')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0032', N'Trịnh Khánh Vy', 'Individual', 'vy.trinh@fashionhub.vn', '+84 938 112 233', N'98 Nguyễn Đình Chiểu, Đa Kao, Quận 1, TP.HCM',
        'Passport', 'C8819201', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0033')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0033', N'Nguyễn Thành Long', 'Individual', 'long.nt@hust.edu.vn', '+84 966 554 433', N'1 Đại Cồ Việt, Bách Khoa, Hai Bà Trưng, Hà Nội',
        'National ID Card', '001096001293', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0034')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0034', N'Vũ Đức Anh', 'Individual', 'anh.vuduc@vietinbank.vn', '+84 982 778 899', N'108 Trần Hưng Đạo, Cửa Nam, Hoàn Kiếm, Hà Nội',
        'Driver''s License', 'DL-HN-88291', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0035')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0035', N'Dương Mỹ Linh', 'Individual', 'linh.duong@danangresort.vn', '+84 905 443 322', N'280 Võ Nguyên Giáp, Mỹ An, Ngũ Hành Sơn, Đà Nẵng',
        'National ID Card', '048092001827', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0036')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0036', N'Trần Bảo Ngọc', 'Individual', 'ngoc.tranbao@cinemavn.com', '+84 919 667 788', N'68 Nam Kỳ Khởi Nghĩa, Bến Thành, Quận 1, TP.HCM',
        'Passport', 'C7719283', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0037')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0037', N'Lâm Chí Dũng', 'Individual', 'dung.lam@canthoagri.vn', '+84 979 221 100', N'12 Hòa Bình, Tân An, Ninh Kiều, Cần Thơ',
        'National ID Card', '092087001928', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0038')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0038', N'Chu Đình Trọng', 'Individual', 'trong.chudinh@haiphongport.vn', '+84 948 119 922', N'5 Hoàng Diệu, Minh Khai, Hồng Bàng, Hải Phòng',
        'National ID Card', '031089002819', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0039')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0039', N'Công ty TNHH Thương Mại Dịch Vụ Nam Á', 'Corporate', 'info@nama-trading.vn', '+84 28 3930 1122', N'40 Phạm Ngọc Thạch, Phường 6, Quận 3, TP.HCM',
        'Driver''s License', 'DL-CORP-4011', '0314882910', 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0040')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0040', N'David Kim', 'Individual', 'david.kim@koreatown.ny', '+1 (555) 234-5544', N'32 W 32nd St, Koreatown, New York, NY 10001',
        'Passport', 'P-KR-992182', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0041')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0041', N'Hannah Schmidt', 'Individual', 'hannah.schmidt@berlin-tech.de', '+1 (555) 887-1122', N'72 Franklin St, Greenpoint, Brooklyn, NY 11222',
        'National ID Card', 'ID-DE-88271', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0042')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0042', N'Ethan Walker', 'Individual', 'ethan.walker@williamsburg.io', '+1 (555) 345-9876', N'240 Bedford Ave, Williamsburg, Brooklyn, NY 11211',
        'Driver''s License', 'DL-NY-24011', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0043')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0043', N'Mia Taylor', 'Individual', 'mia.taylor@longislandcity.net', '+1 (555) 901-2233', N'44-02 23rd St, Long Island City, Queens, NY 11101',
        'Passport', 'P-US-901223', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0044')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0044', N'Noah Wilson', 'Individual', 'noah.wilson@bushwickcraft.com', '+1 (555) 432-8877', N'119 Ingraham St, Bushwick, Brooklyn, NY 11237',
        'National ID Card', 'ID-US-11928', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0045')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0045', N'Ava Thomas', 'Individual', 'ava.thomas@parkslope.org', '+1 (555) 678-1122', N'315 7th Ave, Park Slope, Brooklyn, NY 11215',
        'Driver''s License', 'DL-NY-31577', NULL, 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0046')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0046', N'Brooklyn Roasting Company LLC', 'Corporate', 'orders@brooklynroasting.com', '+1 (555) 888-4422', N'25 Jay St, Dumbo, Brooklyn, NY 11201',
        'Driver''s License', 'DL-CORP-2511', 'TAX-US-25119', 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE CustomerID = 'CUST-0047')
BEGIN
    INSERT INTO dbo.Customer (
        CustomerID, FullName, CustomerType, Email, ContactNumber, Address,
        IdProofType, IdProofNumber, TaxNumber, Status, PasswordHash, CreatedAt, UpdatedAt
    ) VALUES (
        'CUST-0047', N'Manhattan Medical Associates PC', 'Corporate', 'admin@manhattanmedical.org', '+1 (555) 212-9900', N'654 Madison Ave, Suite 800, New York, NY 10065',
        'Passport', 'P-CORP-6540', 'TAX-US-65401', 'Active',
        '$2a$10$KOB51khRySpHpsNDdQDOGunAsrc2N4uY335NNDH04Pf85lkQdcQ1q', '2026-09-01 08:00:00', '2026-09-01 08:00:00'
    );
END;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '2. Thêm đơn hàng mẫu vào dbo.Orders...';

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000016')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000016', 'CUST-0001', 'plan-bb-h30', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 122, Đường số 17, Chi nhánh SH-01', '2026-09-22 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-22 08:10:00', '2026-09-22 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000017')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000017', 'CUST-0002', 'plan-bb-h30', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 129, Đường số 18, Chi nhánh SH-03', '2026-09-27 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-27 09:17:00', '2026-09-27 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000018')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000018', 'CUST-0003', 'plan-bb-h30', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 136, Đường số 19, Chi nhánh SH-01', '2026-09-14 10:24:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-14 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-14 13:00:00',
        '2026-09-14 10:24:00', '2026-09-14 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000019')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000019', 'CUST-0004', 'plan-bb-h30', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 143, Đường số 20, Chi nhánh SH-03', '2026-09-19 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-19 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-19 13:00:00',
        '2026-09-19 11:31:00', '2026-09-19 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000020')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000020', 'CUST-0005', 'plan-bb-h30', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 150, Đường số 1, Chi nhánh SH-01', '2026-09-24 12:38:00', 'Feasible',
        290, N'DP-SH-01-P5', -16.00, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-24 14:00:00', '2026-09-26', NULL,
        5, 5.00, NULL, NULL, 1,
        'B064-000000000011', N'David Chen', '2026-09-23 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-24 12:38:00', '2026-09-24 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000021')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000021', 'CUST-0006', 'plan-bb-h30', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 157, Đường số 2, Chi nhánh SH-03', '2026-09-11 13:45:00', 'Feasible',
        301, N'DP-SH-03-P6', -16.10, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-11 14:00:00', '2026-09-13', NULL,
        1, 0.00, NULL, NULL, 1,
        'B072-000000000012', N'David Chen', '2026-09-10 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-11 13:45:00', '2026-09-11 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000022')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000022', 'CUST-0007', 'plan-bb-h30', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 164, Đường số 3, Chi nhánh SH-01', '2026-09-16 14:52:00', 'Connection Provided',
        248, N'DP-SH-01-MAIN-3', -15.70, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-22 10:00:00', '2026-08-24', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000013', N'Aiden Brooks', '2026-08-21 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-16 14:52:00', '2026-09-16 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000023')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000023', 'CUST-0008', 'plan-bb-h30', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 171, Đường số 4, Chi nhánh SH-03', '2026-09-21 15:59:00', 'Connection Provided',
        57, N'DP-SH-03-MAIN-4', -15.80, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, NULL, 1,
        'B072-000000000014', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-21 15:59:00', '2026-09-21 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000024')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000024', 'CUST-0009', 'plan-bb-h30', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 178, Đường số 5, Chi nhánh SH-01', '2026-09-26 16:16:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-26 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-25 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-26 16:16:00', '2026-09-26 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000025')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000025', 'CUST-0010', 'plan-bb-64', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 185, Đường số 6, Chi nhánh SH-02', '2026-09-13 08:10:00', 'Feasible',
        95, N'DP-SH-02-P2', -16.50, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-13 14:00:00', '2026-09-15', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000015', N'David Chen', '2026-09-12 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-13 08:10:00', '2026-09-13 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000026')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000026', 'CUST-0011', 'plan-bb-64', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 192, Đường số 7, Chi nhánh SH-04', '2026-09-18 09:17:00', 'Connection Provided',
        84, N'DP-SH-04-MAIN-3', -16.10, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-17 10:00:00', '2026-08-19', NULL,
        1, 0.00, NULL, NULL, 1,
        'B081-000000000016', N'Aiden Brooks', '2026-08-16 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-18 09:17:00', '2026-09-18 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000027')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000027', 'CUST-0012', 'plan-bb-64', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 199, Đường số 8, Chi nhánh SH-02', '2026-09-23 10:24:00', 'Connection Provided',
        93, N'DP-SH-02-MAIN-4', -16.20, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-18 10:00:00', '2026-08-20', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000017', N'Aiden Brooks', '2026-08-17 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-23 10:24:00', '2026-09-23 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000028')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000028', 'CUST-0013', 'plan-bb-64', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 206, Đường số 9, Chi nhánh SH-04', '2026-09-10 11:31:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-10 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-09 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-10 11:31:00', '2026-09-10 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000029')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000029', 'CUST-0014', 'plan-bb-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 213, Đường số 10, Chi nhánh SH-02', '2026-09-25 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-25 08:10:00', '2026-09-25 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000030')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000030', 'CUST-0015', 'plan-bb-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 220, Đường số 11, Chi nhánh SH-04', '2026-09-12 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-12 09:17:00', '2026-09-12 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000031')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000031', 'CUST-0016', 'plan-bb-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 227, Đường số 12, Chi nhánh SH-02', '2026-09-17 10:24:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-17 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-17 13:00:00',
        '2026-09-17 10:24:00', '2026-09-17 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000032')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000032', 'CUST-0017', 'plan-bb-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 234, Đường số 13, Chi nhánh SH-04', '2026-09-22 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-22 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-22 13:00:00',
        '2026-09-22 11:31:00', '2026-09-22 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000033')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000033', 'CUST-0018', 'plan-bb-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 241, Đường số 14, Chi nhánh SH-02', '2026-09-27 12:38:00', 'Feasible',
        183, N'DP-SH-02-P2', -17.30, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-27 14:00:00', '2026-09-28', NULL,
        5, 5.00, NULL, NULL, 1,
        'B064-000000000018', N'David Chen', '2026-09-26 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-27 12:38:00', '2026-09-27 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000034')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000034', 'CUST-0019', 'plan-bb-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 248, Đường số 15, Chi nhánh SH-04', '2026-09-14 13:45:00', 'Feasible',
        194, N'DP-SH-04-P3', -17.40, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-14 14:00:00', '2026-09-16', NULL,
        1, 0.00, NULL, NULL, 1,
        'B081-000000000019', N'David Chen', '2026-09-13 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-14 13:45:00', '2026-09-14 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000035')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000035', 'CUST-0020', 'plan-bb-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 255, Đường số 16, Chi nhánh SH-02', '2026-09-19 14:52:00', 'Connection Provided',
        165, N'DP-SH-02-MAIN-4', -17.00, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-22 10:00:00', '2026-08-24', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000020', N'Aiden Brooks', '2026-08-21 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-19 14:52:00', '2026-09-19 14:52:00'
    );
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000036')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000036', 'CUST-0021', 'plan-bb-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 262, Đường số 17, Chi nhánh SH-04', '2026-09-24 15:59:00', 'Connection Provided',
        174, N'DP-SH-04-MAIN-1', -17.10, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, NULL, 1,
        'B081-000000000021', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-24 15:59:00', '2026-09-24 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000037')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000037', 'CUST-0022', 'plan-bb-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 269, Đường số 18, Chi nhánh SH-02', '2026-09-11 16:16:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-11 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-10 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-11 16:16:00', '2026-09-11 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000038')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000038', 'CUST-0023', 'plan-bb-128', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 276, Đường số 19, Chi nhánh SH-03', '2026-09-16 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-16 08:10:00', '2026-09-16 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000039')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000039', 'CUST-0024', 'plan-bb-128', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 283, Đường số 20, Chi nhánh SH-01', '2026-09-21 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-21 09:17:00', '2026-09-21 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000040')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000040', 'CUST-0025', 'plan-bb-128', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 290, Đường số 1, Chi nhánh SH-03', '2026-09-26 10:24:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-26 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-26 13:00:00',
        '2026-09-26 10:24:00', '2026-09-26 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000041')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000041', 'CUST-0026', 'plan-bb-128', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 297, Đường số 2, Chi nhánh SH-01', '2026-09-13 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-13 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-13 13:00:00',
        '2026-09-13 11:31:00', '2026-09-13 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000042')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000042', 'CUST-0027', 'plan-bb-128', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 304, Đường số 3, Chi nhánh SH-03', '2026-09-18 12:38:00', 'Feasible',
        282, N'DP-SH-03-P3', -18.20, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-18 14:00:00', '2026-09-20', NULL,
        5, 5.00, NULL, NULL, 1,
        'B072-000000000022', N'David Chen', '2026-09-17 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-18 12:38:00', '2026-09-18 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000043')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000043', 'CUST-0028', 'plan-bb-128', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 11, Đường số 4, Chi nhánh SH-01', '2026-09-23 13:45:00', 'Feasible',
        293, N'DP-SH-01-P4', -18.30, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-23 14:00:00', '2026-09-25', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000023', N'David Chen', '2026-09-22 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-23 13:45:00', '2026-09-23 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000044')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000044', 'CUST-0029', 'plan-bb-128', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 18, Đường số 5, Chi nhánh SH-03', '2026-09-10 14:52:00', 'Connection Provided',
        246, N'DP-SH-03-MAIN-1', -17.90, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-22 10:00:00', '2026-08-24', NULL,
        1, 0.00, NULL, NULL, 1,
        'B072-000000000024', N'Aiden Brooks', '2026-08-21 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-10 14:52:00', '2026-09-10 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000045')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000045', 'CUST-0030', 'plan-bb-128', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 25, Đường số 6, Chi nhánh SH-01', '2026-09-15 15:59:00', 'Connection Provided',
        55, N'DP-SH-01-MAIN-2', -18.00, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000025', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-15 15:59:00', '2026-09-15 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000046')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000046', 'CUST-0031', 'plan-bb-128', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 32, Đường số 7, Chi nhánh SH-03', '2026-09-20 16:16:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-20 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-19 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-20 16:16:00', '2026-09-20 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000047')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000047', 'CUST-0032', 'plan-bb-h120', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 39, Đường số 8, Chi nhánh SH-04', '2026-09-25 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-25 08:10:00', '2026-09-25 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000048')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000048', 'CUST-0033', 'plan-bb-h120', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 46, Đường số 9, Chi nhánh SH-02', '2026-09-12 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-12 09:17:00', '2026-09-12 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000049')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000049', 'CUST-0034', 'plan-bb-h120', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 53, Đường số 10, Chi nhánh SH-04', '2026-09-17 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-17 10:24:00', '2026-09-17 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000050')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000050', 'CUST-0035', 'plan-bb-h120', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 60, Đường số 11, Chi nhánh SH-02', '2026-09-22 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-22 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-22 13:00:00',
        '2026-09-22 11:31:00', '2026-09-22 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000051')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000051', 'CUST-0036', 'plan-bb-h120', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 67, Đường số 12, Chi nhánh SH-04', '2026-09-27 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        5, 5.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-27 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-27 13:00:00',
        '2026-09-27 12:38:00', '2026-09-27 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000052')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000052', 'CUST-0037', 'plan-bb-h120', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 74, Đường số 13, Chi nhánh SH-02', '2026-09-14 13:45:00', 'Feasible',
        142, N'DP-SH-02-P5', -19.20, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-14 14:00:00', '2026-09-16', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000026', N'David Chen', '2026-09-13 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-14 13:45:00', '2026-09-14 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000053')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000053', 'CUST-0038', 'plan-bb-h120', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 81, Đường số 14, Chi nhánh SH-04', '2026-09-19 14:52:00', 'Feasible',
        153, N'DP-SH-04-P6', -19.30, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-19 14:00:00', '2026-09-21', NULL,
        1, 0.00, NULL, NULL, 1,
        'B081-000000000027', N'David Chen', '2026-09-18 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-19 14:52:00', '2026-09-19 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000054')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000054', 'CUST-0039', 'plan-bb-h120', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 88, Đường số 15, Chi nhánh SH-02', '2026-09-24 15:59:00', 'Connection Provided',
        136, N'DP-SH-02-MAIN-3', -13.90, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, NULL, 1,
        'B064-000000000028', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-24 15:59:00', '2026-09-24 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000055')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000055', 'CUST-0040', 'plan-bb-h120', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 95, Đường số 16, Chi nhánh SH-04', '2026-09-11 16:16:00', 'Connection Provided',
        145, N'DP-SH-04-MAIN-4', -14.00, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, NULL, 1,
        'B081-000000000029', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-11 16:16:00', '2026-09-11 16:16:00'
    );
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'B0000000056')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'B0000000056', 'CUST-0041', 'plan-bb-h120', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 102, Đường số 17, Chi nhánh SH-02', '2026-09-16 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-16 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-15 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-16 08:23:00', '2026-09-16 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000007')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000007', 'CUST-0042', 'plan-du-h10', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 59, Đường số 8, Chi nhánh SH-04', '2026-09-13 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-13 08:10:00', '2026-09-13 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000008')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000008', 'CUST-0043', 'plan-du-h10', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 66, Đường số 9, Chi nhánh SH-02', '2026-09-18 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-18 09:17:00', '2026-09-18 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000009')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000009', 'CUST-0044', 'plan-du-h10', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 73, Đường số 10, Chi nhánh SH-04', '2026-09-23 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-23 10:24:00', '2026-09-23 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000010')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000010', 'CUST-0045', 'plan-du-h10', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 80, Đường số 11, Chi nhánh SH-02', '2026-09-10 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-10 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-10 13:00:00',
        '2026-09-10 11:31:00', '2026-09-10 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000011')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000011', 'CUST-0046', 'plan-du-h10', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 87, Đường số 12, Chi nhánh SH-04', '2026-09-15 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-15 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-15 13:00:00',
        '2026-09-15 12:38:00', '2026-09-15 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000012')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000012', 'CUST-0047', 'plan-du-h10', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 94, Đường số 13, Chi nhánh SH-02', '2026-09-20 13:45:00', 'Feasible',
        202, N'DP-SH-02-P5', -15.20, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-20 14:00:00', '2026-09-22', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000030', N'David Chen', '2026-09-19 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-20 13:45:00', '2026-09-20 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000013')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000013', 'CUST-0001', 'plan-du-h10', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 101, Đường số 14, Chi nhánh SH-04', '2026-09-25 14:52:00', 'Feasible',
        213, N'DP-SH-04-P6', -15.30, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-25 14:00:00', '2026-09-27', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000031', N'David Chen', '2026-09-24 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-25 14:52:00', '2026-09-25 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000014')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000014', 'CUST-0002', 'plan-du-h10', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 108, Đường số 15, Chi nhánh SH-02', '2026-09-12 15:59:00', 'Connection Provided',
        176, N'DP-SH-02-MAIN-3', -14.90, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000032', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-12 15:59:00', '2026-09-12 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000015')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000015', 'CUST-0003', 'plan-du-h10', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 115, Đường số 16, Chi nhánh SH-04', '2026-09-17 16:16:00', 'Connection Provided',
        185, N'DP-SH-04-MAIN-4', -15.00, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000033', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-17 16:16:00', '2026-09-17 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000016')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000016', 'CUST-0004', 'plan-du-h10', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 122, Đường số 17, Chi nhánh SH-02', '2026-09-22 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-22 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-21 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-22 08:23:00', '2026-09-22 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000017')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000017', 'CUST-0005', 'plan-du-28', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 129, Đường số 18, Chi nhánh SH-02', '2026-09-25 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-25 08:10:00', '2026-09-25 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000018')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000018', 'CUST-0006', 'plan-du-28', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 136, Đường số 19, Chi nhánh SH-04', '2026-09-12 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-12 09:17:00', '2026-09-12 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000019')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000019', 'CUST-0007', 'plan-du-28', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 143, Đường số 20, Chi nhánh SH-02', '2026-09-17 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-17 10:24:00', '2026-09-17 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000020')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000020', 'CUST-0008', 'plan-du-28', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 150, Đường số 1, Chi nhánh SH-04', '2026-09-22 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-22 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-22 13:00:00',
        '2026-09-22 11:31:00', '2026-09-22 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000021')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000021', 'CUST-0009', 'plan-du-28', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 157, Đường số 2, Chi nhánh SH-02', '2026-09-27 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-27 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-27 13:00:00',
        '2026-09-27 12:38:00', '2026-09-27 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000022')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000022', 'CUST-0010', 'plan-du-28', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 164, Đường số 3, Chi nhánh SH-04', '2026-09-14 13:45:00', 'Feasible',
        312, N'DP-SH-04-P7', -16.20, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-14 14:00:00', '2026-09-16', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000034', N'David Chen', '2026-09-13 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-14 13:45:00', '2026-09-14 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000023')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000023', 'CUST-0011', 'plan-du-28', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 171, Đường số 4, Chi nhánh SH-02', '2026-09-19 14:52:00', 'Feasible',
        73, N'DP-SH-02-P8', -16.30, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-19 14:00:00', '2026-09-21', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000035', N'David Chen', '2026-09-18 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-19 14:52:00', '2026-09-19 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000024')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000024', 'CUST-0012', 'plan-du-28', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 178, Đường số 5, Chi nhánh SH-04', '2026-09-24 15:59:00', 'Connection Provided',
        66, N'DP-SH-04-MAIN-1', -15.90, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000036', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-24 15:59:00', '2026-09-24 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000025')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000025', 'CUST-0013', 'plan-du-28', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 185, Đường số 6, Chi nhánh SH-02', '2026-09-11 16:16:00', 'Connection Provided',
        75, N'DP-SH-02-MAIN-2', -16.00, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000037', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-11 16:16:00', '2026-09-11 16:16:00'
    );
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000026')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000026', 'CUST-0014', 'plan-du-28', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 192, Đường số 7, Chi nhánh SH-04', '2026-09-16 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-16 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-15 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-16 08:23:00', '2026-09-16 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000027')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000027', 'CUST-0015', 'plan-du-56', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 199, Đường số 8, Chi nhánh SH-04', '2026-09-19 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-19 08:10:00', '2026-09-19 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000028')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000028', 'CUST-0016', 'plan-du-56', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 206, Đường số 9, Chi nhánh SH-02', '2026-09-24 09:17:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-24 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-24 13:00:00',
        '2026-09-24 09:17:00', '2026-09-24 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000029')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000029', 'CUST-0017', 'plan-du-56', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 213, Đường số 10, Chi nhánh SH-04', '2026-09-11 10:24:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-11 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-11 13:00:00',
        '2026-09-11 10:24:00', '2026-09-11 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000030')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000030', 'CUST-0018', 'plan-du-56', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 220, Đường số 11, Chi nhánh SH-02', '2026-09-16 11:31:00', 'Feasible',
        150, N'DP-SH-02-P7', -17.00, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-16 14:00:00', '2026-09-18', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000038', N'David Chen', '2026-09-15 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-16 11:31:00', '2026-09-16 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000031')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000031', 'CUST-0019', 'plan-du-56', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 227, Đường số 12, Chi nhánh SH-04', '2026-09-21 12:38:00', 'Feasible',
        161, N'DP-SH-04-P8', -17.10, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-21 14:00:00', '2026-09-23', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000039', N'David Chen', '2026-09-20 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-21 12:38:00', '2026-09-21 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000032')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000032', 'CUST-0020', 'plan-du-56', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 234, Đường số 13, Chi nhánh SH-02', '2026-09-26 13:45:00', 'Connection Provided',
        138, N'DP-SH-02-MAIN-1', -16.70, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-21 10:00:00', '2026-08-23', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000040', N'Aiden Brooks', '2026-08-20 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-26 13:45:00', '2026-09-26 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000033')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000033', 'CUST-0021', 'plan-du-56', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 241, Đường số 14, Chi nhánh SH-04', '2026-09-13 14:52:00', 'Connection Provided',
        147, N'DP-SH-04-MAIN-2', -16.80, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-22 10:00:00', '2026-08-24', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000041', N'Aiden Brooks', '2026-08-21 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-13 14:52:00', '2026-09-13 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000034')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000034', 'CUST-0022', 'plan-du-56', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 248, Đường số 15, Chi nhánh SH-02', '2026-09-18 15:59:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-18 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-17 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-18 15:59:00', '2026-09-18 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000035')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000035', 'CUST-0023', 'plan-du-h30', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 255, Đường số 16, Chi nhánh SH-04', '2026-09-25 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-25 08:10:00', '2026-09-25 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000036')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000036', 'CUST-0024', 'plan-du-h30', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 262, Đường số 17, Chi nhánh SH-02', '2026-09-12 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-12 09:17:00', '2026-09-12 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000037')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000037', 'CUST-0025', 'plan-du-h30', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 269, Đường số 18, Chi nhánh SH-04', '2026-09-17 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-17 10:24:00', '2026-09-17 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000038')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000038', 'CUST-0026', 'plan-du-h30', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 276, Đường số 19, Chi nhánh SH-02', '2026-09-22 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-22 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-22 13:00:00',
        '2026-09-22 11:31:00', '2026-09-22 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000039')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000039', 'CUST-0027', 'plan-du-h30', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 283, Đường số 20, Chi nhánh SH-04', '2026-09-27 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-27 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-27 13:00:00',
        '2026-09-27 12:38:00', '2026-09-27 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000040')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000040', 'CUST-0028', 'plan-du-h30', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 290, Đường số 1, Chi nhánh SH-02', '2026-09-14 13:45:00', 'Feasible',
        260, N'DP-SH-02-P1', -18.00, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-14 14:00:00', '2026-09-16', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000042', N'David Chen', '2026-09-13 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-14 13:45:00', '2026-09-14 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000041')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000041', 'CUST-0029', 'plan-du-h30', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 297, Đường số 2, Chi nhánh SH-04', '2026-09-19 14:52:00', 'Feasible',
        271, N'DP-SH-04-P2', -18.10, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-19 14:00:00', '2026-09-21', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000043', N'David Chen', '2026-09-18 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-19 14:52:00', '2026-09-19 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000042')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000042', 'CUST-0030', 'plan-du-h30', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 304, Đường số 3, Chi nhánh SH-02', '2026-09-24 15:59:00', 'Connection Provided',
        228, N'DP-SH-02-MAIN-3', -17.70, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000044', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-24 15:59:00', '2026-09-24 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000043')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000043', 'CUST-0031', 'plan-du-h30', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 11, Đường số 4, Chi nhánh SH-04', '2026-09-11 16:16:00', 'Connection Provided',
        237, N'DP-SH-04-MAIN-4', -17.80, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000045', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-11 16:16:00', '2026-09-11 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000044')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000044', 'CUST-0032', 'plan-du-h30', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 18, Đường số 5, Chi nhánh SH-02', '2026-09-16 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-16 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-15 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-16 08:23:00', '2026-09-16 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000045')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000045', 'CUST-0033', 'plan-du-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 25, Đường số 6, Chi nhánh SH-02', '2026-09-19 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-19 08:10:00', '2026-09-19 08:10:00'
    );
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000046')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000046', 'CUST-0034', 'plan-du-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 32, Đường số 7, Chi nhánh SH-04', '2026-09-24 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-24 09:17:00', '2026-09-24 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000047')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000047', 'CUST-0035', 'plan-du-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 39, Đường số 8, Chi nhánh SH-02', '2026-09-11 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-11 10:24:00', '2026-09-11 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000048')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000048', 'CUST-0036', 'plan-du-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 46, Đường số 9, Chi nhánh SH-04', '2026-09-16 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-16 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-16 13:00:00',
        '2026-09-16 11:31:00', '2026-09-16 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000049')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000049', 'CUST-0037', 'plan-du-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 53, Đường số 10, Chi nhánh SH-02', '2026-09-21 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-21 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-21 13:00:00',
        '2026-09-21 12:38:00', '2026-09-21 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000050')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000050', 'CUST-0038', 'plan-du-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 60, Đường số 11, Chi nhánh SH-04', '2026-09-26 13:45:00', 'Feasible',
        120, N'DP-SH-04-P3', -19.00, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-26 14:00:00', '2026-09-28', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000046', N'David Chen', '2026-09-25 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-26 13:45:00', '2026-09-26 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000051')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000051', 'CUST-0039', 'plan-du-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 67, Đường số 12, Chi nhánh SH-02', '2026-09-13 14:52:00', 'Feasible',
        131, N'DP-SH-02-P4', -19.10, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-13 14:00:00', '2026-09-15', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000047', N'David Chen', '2026-09-12 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-13 14:52:00', '2026-09-13 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000052')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000052', 'CUST-0040', 'plan-du-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 74, Đường số 13, Chi nhánh SH-04', '2026-09-18 15:59:00', 'Connection Provided',
        118, N'DP-SH-04-MAIN-1', -13.70, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000048', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-18 15:59:00', '2026-09-18 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000053')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000053', 'CUST-0041', 'plan-du-h60', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 81, Đường số 14, Chi nhánh SH-02', '2026-09-23 16:16:00', 'Connection Provided',
        127, N'DP-SH-02-MAIN-2', -13.80, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000049', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-23 16:16:00', '2026-09-23 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000054')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000054', 'CUST-0042', 'plan-du-h60', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 88, Đường số 15, Chi nhánh SH-04', '2026-09-10 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-10 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-09 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-10 08:23:00', '2026-09-10 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000055')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000055', 'CUST-0043', 'plan-du-h100', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 95, Đường số 16, Chi nhánh SH-04', '2026-09-13 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-13 08:10:00', '2026-09-13 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000056')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000056', 'CUST-0044', 'plan-du-h100', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 102, Đường số 17, Chi nhánh SH-02', '2026-09-18 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-18 09:17:00', '2026-09-18 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000057')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000057', 'CUST-0045', 'plan-du-h100', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 109, Đường số 18, Chi nhánh SH-04', '2026-09-23 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-23 10:24:00', '2026-09-23 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000058')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000058', 'CUST-0046', 'plan-du-h100', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 116, Đường số 19, Chi nhánh SH-02', '2026-09-10 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-10 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-10 13:00:00',
        '2026-09-10 11:31:00', '2026-09-10 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000059')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000059', 'CUST-0047', 'plan-du-h100', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 123, Đường số 20, Chi nhánh SH-04', '2026-09-15 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-15 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-15 13:00:00',
        '2026-09-15 12:38:00', '2026-09-15 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000060')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000060', 'CUST-0001', 'plan-du-h100', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 130, Đường số 1, Chi nhánh SH-02', '2026-09-20 13:45:00', 'Feasible',
        230, N'DP-SH-02-P5', -14.00, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-20 14:00:00', '2026-09-22', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000050', N'David Chen', '2026-09-19 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-20 13:45:00', '2026-09-20 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000061')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000061', 'CUST-0002', 'plan-du-h100', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 137, Đường số 2, Chi nhánh SH-04', '2026-09-25 14:52:00', 'Feasible',
        241, N'DP-SH-04-P6', -14.10, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-25 14:00:00', '2026-09-27', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000051', N'David Chen', '2026-09-24 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-25 14:52:00', '2026-09-25 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000062')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000062', 'CUST-0003', 'plan-du-h100', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 144, Đường số 3, Chi nhánh SH-02', '2026-09-12 15:59:00', 'Connection Provided',
        208, N'DP-SH-02-MAIN-3', -14.70, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, 1,
        'D064-000000000052', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-12 15:59:00', '2026-09-12 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000063')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000063', 'CUST-0004', 'plan-du-h100', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 151, Đường số 4, Chi nhánh SH-04', '2026-09-17 16:16:00', 'Connection Provided',
        217, N'DP-SH-04-MAIN-4', -14.80, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, 1,
        'D081-000000000053', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-17 16:16:00', '2026-09-17 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'D0000000064')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'D0000000064', 'CUST-0005', 'plan-du-h100', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 158, Đường số 5, Chi nhánh SH-02', '2026-09-22 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-22 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-21 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-22 08:23:00', '2026-09-22 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000012')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000012', 'CUST-0006', 'plan-ll-loc-a', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 94, Đường số 13, Chi nhánh SH-01', '2026-09-10 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-10 08:10:00', '2026-09-10 08:10:00'
    );
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000013')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000013', 'CUST-0007', 'plan-ll-loc-a', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 101, Đường số 14, Chi nhánh SH-03', '2026-09-15 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-15 09:17:00', '2026-09-15 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000014')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000014', 'CUST-0008', 'plan-ll-loc-a', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 108, Đường số 15, Chi nhánh SH-01', '2026-09-20 10:24:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-20 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-20 13:00:00',
        '2026-09-20 10:24:00', '2026-09-20 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000015')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000015', 'CUST-0009', 'plan-ll-loc-a', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 115, Đường số 16, Chi nhánh SH-03', '2026-09-25 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        5, 5.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-25 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-25 13:00:00',
        '2026-09-25 11:31:00', '2026-09-25 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000016')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000016', 'CUST-0010', 'plan-ll-loc-a', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 122, Đường số 17, Chi nhánh SH-01', '2026-09-12 12:38:00', 'Feasible',
        246, N'DP-SH-01-P1', -15.60, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-12 14:00:00', '2026-09-14', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000054', N'David Chen', '2026-09-11 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-12 12:38:00', '2026-09-12 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000017')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000017', 'CUST-0011', 'plan-ll-loc-a', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 129, Đường số 18, Chi nhánh SH-03', '2026-09-17 13:45:00', 'Feasible',
        257, N'DP-SH-03-P2', -15.70, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-17 14:00:00', '2026-09-19', NULL,
        1, 0.00, NULL, 1, NULL,
        'T072-000000000055', N'David Chen', '2026-09-16 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-17 13:45:00', '2026-09-17 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000018')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000018', 'CUST-0012', 'plan-ll-loc-a', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 136, Đường số 19, Chi nhánh SH-01', '2026-09-22 14:52:00', 'Connection Provided',
        212, N'DP-SH-01-MAIN-3', -15.30, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-22 10:00:00', '2026-08-24', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000056', N'Aiden Brooks', '2026-08-21 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-22 14:52:00', '2026-09-22 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000019')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000019', 'CUST-0013', 'plan-ll-loc-a', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 143, Đường số 20, Chi nhánh SH-03', '2026-09-27 15:59:00', 'Connection Provided',
        221, N'DP-SH-03-MAIN-4', -15.40, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, NULL,
        'T072-000000000057', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-27 15:59:00', '2026-09-27 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000020')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000020', 'CUST-0014', 'plan-ll-loc-a', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 150, Đường số 1, Chi nhánh SH-01', '2026-09-14 16:16:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-14 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-13 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-14 16:16:00', '2026-09-14 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000021')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000021', 'CUST-0015', 'plan-ll-loc-b', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 157, Đường số 2, Chi nhánh SH-02', '2026-09-19 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-19 08:10:00', '2026-09-19 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000022')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000022', 'CUST-0016', 'plan-ll-loc-b', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 164, Đường số 3, Chi nhánh SH-04', '2026-09-24 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-24 09:17:00', '2026-09-24 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000023')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000023', 'CUST-0017', 'plan-ll-loc-b', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 171, Đường số 4, Chi nhánh SH-02', '2026-09-11 10:24:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-11 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-11 13:00:00',
        '2026-09-11 10:24:00', '2026-09-11 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000024')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000024', 'CUST-0018', 'plan-ll-loc-b', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 178, Đường số 5, Chi nhánh SH-04', '2026-09-16 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        5, 5.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-16 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-16 13:00:00',
        '2026-09-16 11:31:00', '2026-09-16 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000025')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000025', 'CUST-0019', 'plan-ll-loc-b', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 185, Đường số 6, Chi nhánh SH-02', '2026-09-21 12:38:00', 'Feasible',
        95, N'DP-SH-02-P2', -16.50, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-21 14:00:00', '2026-09-23', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000058', N'David Chen', '2026-09-20 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-21 12:38:00', '2026-09-21 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000026')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000026', 'CUST-0020', 'plan-ll-loc-b', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 192, Đường số 7, Chi nhánh SH-04', '2026-09-26 13:45:00', 'Feasible',
        106, N'DP-SH-04-P3', -16.60, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-26 14:00:00', '2026-09-28', NULL,
        1, 0.00, NULL, 1, NULL,
        'T081-000000000059', N'David Chen', '2026-09-25 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-26 13:45:00', '2026-09-26 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000027')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000027', 'CUST-0021', 'plan-ll-loc-b', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 199, Đường số 8, Chi nhánh SH-02', '2026-09-13 14:52:00', 'Connection Provided',
        93, N'DP-SH-02-MAIN-4', -16.20, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-22 10:00:00', '2026-08-24', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000060', N'Aiden Brooks', '2026-08-21 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-13 14:52:00', '2026-09-13 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000028')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000028', 'CUST-0022', 'plan-ll-loc-b', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 206, Đường số 9, Chi nhánh SH-04', '2026-09-18 15:59:00', 'Connection Provided',
        102, N'DP-SH-04-MAIN-1', -16.30, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, NULL,
        'T081-000000000061', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-18 15:59:00', '2026-09-18 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000029')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000029', 'CUST-0023', 'plan-ll-loc-b', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 213, Đường số 10, Chi nhánh SH-02', '2026-09-23 16:16:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-23 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-22 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-23 16:16:00', '2026-09-23 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000030')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000030', 'CUST-0024', 'plan-ll-loc-u', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 220, Đường số 11, Chi nhánh SH-03', '2026-09-10 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-10 08:10:00', '2026-09-10 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000031')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000031', 'CUST-0025', 'plan-ll-loc-u', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 227, Đường số 12, Chi nhánh SH-01', '2026-09-15 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-15 09:17:00', '2026-09-15 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000032')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000032', 'CUST-0026', 'plan-ll-loc-u', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 234, Đường số 13, Chi nhánh SH-03', '2026-09-20 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-20 10:24:00', '2026-09-20 10:24:00'
    );
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000033')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000033', 'CUST-0027', 'plan-ll-loc-u', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 241, Đường số 14, Chi nhánh SH-01', '2026-09-25 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        5, 5.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-25 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-25 13:00:00',
        '2026-09-25 11:31:00', '2026-09-25 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000034')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000034', 'CUST-0028', 'plan-ll-loc-u', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 248, Đường số 15, Chi nhánh SH-03', '2026-09-12 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-12 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-12 13:00:00',
        '2026-09-12 12:38:00', '2026-09-12 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000035')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000035', 'CUST-0029', 'plan-ll-loc-u', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 255, Đường số 16, Chi nhánh SH-01', '2026-09-17 13:45:00', 'Feasible',
        205, N'DP-SH-01-P4', -17.50, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-17 14:00:00', '2026-09-19', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000062', N'David Chen', '2026-09-16 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-17 13:45:00', '2026-09-17 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000036')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000036', 'CUST-0030', 'plan-ll-loc-u', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 262, Đường số 17, Chi nhánh SH-03', '2026-09-22 14:52:00', 'Feasible',
        216, N'DP-SH-03-P5', -17.60, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-22 14:00:00', '2026-09-24', NULL,
        1, 0.00, NULL, 1, NULL,
        'T072-000000000063', N'David Chen', '2026-09-21 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-22 14:52:00', '2026-09-22 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000037')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000037', 'CUST-0031', 'plan-ll-loc-u', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 269, Đường số 18, Chi nhánh SH-01', '2026-09-27 15:59:00', 'Connection Provided',
        183, N'DP-SH-01-MAIN-2', -17.20, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000064', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-27 15:59:00', '2026-09-27 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000038')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000038', 'CUST-0032', 'plan-ll-loc-u', 'SH-03', 'emp-02',
        N'Địa chỉ lắp đặt: Số 276, Đường số 19, Chi nhánh SH-03', '2026-09-14 16:16:00', 'Connection Provided',
        192, N'DP-SH-03-MAIN-3', -17.30, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, NULL,
        'T072-000000000065', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-14 16:16:00', '2026-09-14 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000039')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000039', 'CUST-0033', 'plan-ll-loc-u', 'SH-01', 'emp-05',
        N'Địa chỉ lắp đặt: Số 283, Đường số 20, Chi nhánh SH-01', '2026-09-19 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-19 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-18 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-19 08:23:00', '2026-09-19 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000040')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000040', 'CUST-0034', 'plan-ll-std-m', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 290, Đường số 1, Chi nhánh SH-01', '2026-09-22 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-22 08:10:00', '2026-09-22 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000041')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000041', 'CUST-0035', 'plan-ll-std-m', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 297, Đường số 2, Chi nhánh SH-03', '2026-09-27 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-27 09:17:00', '2026-09-27 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000042')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000042', 'CUST-0036', 'plan-ll-std-m', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 304, Đường số 3, Chi nhánh SH-01', '2026-09-14 10:24:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-14 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-14 13:00:00',
        '2026-09-14 10:24:00', '2026-09-14 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000043')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000043', 'CUST-0037', 'plan-ll-std-m', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 11, Đường số 4, Chi nhánh SH-03', '2026-09-19 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        5, 5.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-19 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-19 13:00:00',
        '2026-09-19 11:31:00', '2026-09-19 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000044')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000044', 'CUST-0038', 'plan-ll-std-m', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 18, Đường số 5, Chi nhánh SH-01', '2026-09-24 12:38:00', 'Feasible',
        304, N'DP-SH-01-P5', -18.40, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-24 14:00:00', '2026-09-26', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000066', N'David Chen', '2026-09-23 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-24 12:38:00', '2026-09-24 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000045')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000045', 'CUST-0039', 'plan-ll-std-m', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 25, Đường số 6, Chi nhánh SH-03', '2026-09-11 13:45:00', 'Feasible',
        315, N'DP-SH-03-P6', -18.50, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-11 14:00:00', '2026-09-13', NULL,
        1, 0.00, NULL, 1, NULL,
        'T072-000000000067', N'David Chen', '2026-09-10 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-11 13:45:00', '2026-09-11 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000046')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000046', 'CUST-0040', 'plan-ll-std-m', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 32, Đường số 7, Chi nhánh SH-01', '2026-09-16 14:52:00', 'Connection Provided',
        64, N'DP-SH-01-MAIN-3', -18.10, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-22 10:00:00', '2026-08-24', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000068', N'Aiden Brooks', '2026-08-21 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-16 14:52:00', '2026-09-16 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000047')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000047', 'CUST-0041', 'plan-ll-std-m', 'SH-03', 'emp-05',
        N'Địa chỉ lắp đặt: Số 39, Đường số 8, Chi nhánh SH-03', '2026-09-21 15:59:00', 'Connection Provided',
        73, N'DP-SH-03-MAIN-4', -18.20, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, NULL,
        'T072-000000000069', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-21 15:59:00', '2026-09-21 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000048')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000048', 'CUST-0042', 'plan-ll-std-m', 'SH-01', 'emp-02',
        N'Địa chỉ lắp đặt: Số 46, Đường số 9, Chi nhánh SH-01', '2026-09-26 16:16:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-26 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-25 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-26 16:16:00', '2026-09-26 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000049')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000049', 'CUST-0043', 'plan-ll-std-h', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 53, Đường số 10, Chi nhánh SH-02', '2026-09-13 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-13 08:10:00', '2026-09-13 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000050')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000050', 'CUST-0044', 'plan-ll-std-h', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 60, Đường số 11, Chi nhánh SH-04', '2026-09-18 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-18 09:17:00', '2026-09-18 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000051')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000051', 'CUST-0045', 'plan-ll-std-h', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 67, Đường số 12, Chi nhánh SH-02', '2026-09-23 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-23 10:24:00', '2026-09-23 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000052')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000052', 'CUST-0046', 'plan-ll-std-h', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 74, Đường số 13, Chi nhánh SH-04', '2026-09-10 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        5, 5.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-10 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-10 13:00:00',
        '2026-09-10 11:31:00', '2026-09-10 11:31:00'
    );
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000053')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000053', 'CUST-0047', 'plan-ll-std-h', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 81, Đường số 14, Chi nhánh SH-02', '2026-09-15 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-15 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-15 13:00:00',
        '2026-09-15 12:38:00', '2026-09-15 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000054')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000054', 'CUST-0001', 'plan-ll-std-h', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 88, Đường số 15, Chi nhánh SH-04', '2026-09-20 13:45:00', 'Feasible',
        164, N'DP-SH-04-P7', -19.40, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-20 14:00:00', '2026-09-22', NULL,
        1, 0.00, NULL, 1, NULL,
        'T081-000000000070', N'David Chen', '2026-09-19 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-20 13:45:00', '2026-09-20 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000055')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000055', 'CUST-0002', 'plan-ll-std-h', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 95, Đường số 16, Chi nhánh SH-02', '2026-09-25 14:52:00', 'Feasible',
        175, N'DP-SH-02-P8', -19.50, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-25 14:00:00', '2026-09-27', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000071', N'David Chen', '2026-09-24 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-25 14:52:00', '2026-09-25 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000056')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000056', 'CUST-0003', 'plan-ll-std-h', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 102, Đường số 17, Chi nhánh SH-04', '2026-09-12 15:59:00', 'Connection Provided',
        154, N'DP-SH-04-MAIN-1', -14.10, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, NULL,
        'T081-000000000072', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-12 15:59:00', '2026-09-12 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000057')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000057', 'CUST-0004', 'plan-ll-std-h', 'SH-02', 'emp-02',
        N'Địa chỉ lắp đặt: Số 109, Đường số 18, Chi nhánh SH-02', '2026-09-17 16:16:00', 'Connection Provided',
        163, N'DP-SH-02-MAIN-2', -14.20, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000073', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-17 16:16:00', '2026-09-17 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000058')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000058', 'CUST-0005', 'plan-ll-std-h', 'SH-04', 'emp-05',
        N'Địa chỉ lắp đặt: Số 116, Đường số 19, Chi nhánh SH-04', '2026-09-22 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-22 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-21 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-22 08:23:00', '2026-09-22 08:23:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000059')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000059', 'CUST-0006', 'plan-ll-std-y', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 123, Đường số 20, Chi nhánh SH-04', '2026-09-25 08:10:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-25 08:10:00', '2026-09-25 08:10:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000060')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000060', 'CUST-0007', 'plan-ll-std-y', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 130, Đường số 1, Chi nhánh SH-02', '2026-09-12 09:17:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-12 09:17:00', '2026-09-12 09:17:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000061')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000061', 'CUST-0008', 'plan-ll-std-y', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 137, Đường số 2, Chi nhánh SH-04', '2026-09-17 10:24:00', 'PendingRetail',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, NULL, NULL, N'Khách hàng đăng ký trực tuyến qua cổng Self-Service, đang chờ duyệt hồ sơ tại quầy.',
        NULL, NULL, NULL,
        '2026-09-17 10:24:00', '2026-09-17 10:24:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000062')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000062', 'CUST-0009', 'plan-ll-std-y', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 144, Đường số 3, Chi nhánh SH-02', '2026-09-22 11:31:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        5, 5.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-22 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-22 13:00:00',
        '2026-09-22 11:31:00', '2026-09-22 11:31:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000063')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000063', 'CUST-0010', 'plan-ll-std-y', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 151, Đường số 4, Chi nhánh SH-04', '2026-09-27 12:38:00', 'Pending',
        NULL, NULL, NULL, NULL,
        NULL, NULL, NULL, NULL,
        1, 0.00, NULL, NULL, NULL,
        NULL, N'David Chen', '2026-09-27 11:30:00', N'Đã xác minh hồ sơ và thanh toán tiền cọc, chuyển giao Kỹ thuật viên khảo sát tuyến cáp.',
        N'Marcus Ramirez', '+1 (555) 456-7890', '2026-09-27 13:00:00',
        '2026-09-27 12:38:00', '2026-09-27 12:38:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000064')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000064', 'CUST-0011', 'plan-ll-std-y', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 158, Đường số 5, Chi nhánh SH-02', '2026-09-14 13:45:00', 'Feasible',
        274, N'DP-SH-02-P1', -14.40, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-14 14:00:00', '2026-09-16', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000074', N'David Chen', '2026-09-13 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-14 13:45:00', '2026-09-14 13:45:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000065')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000065', 'CUST-0012', 'plan-ll-std-y', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 165, Đường số 6, Chi nhánh SH-04', '2026-09-19 14:52:00', 'Feasible',
        285, N'DP-SH-04-P2', -14.50, N'Tuyến cáp đạt tiêu chuẩn suy hao. Hộp chia cổng còn vị trí trống, đủ điều kiện thi công.',
        'emp-03', '2026-09-19 14:00:00', '2026-09-21', NULL,
        1, 0.00, NULL, 1, NULL,
        'T081-000000000075', N'David Chen', '2026-09-18 10:00:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-19 14:52:00', '2026-09-19 14:52:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000066')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000066', 'CUST-0013', 'plan-ll-std-y', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 172, Đường số 7, Chi nhánh SH-02', '2026-09-24 15:59:00', 'Connection Provided',
        244, N'DP-SH-02-MAIN-3', -15.10, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-23 10:00:00', '2026-08-25', NULL,
        1, 0.00, NULL, 1, NULL,
        'T064-000000000076', N'Aiden Brooks', '2026-08-22 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-24 15:59:00', '2026-09-24 15:59:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000067')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000067', 'CUST-0014', 'plan-ll-std-y', 'SH-04', 'emp-02',
        N'Địa chỉ lắp đặt: Số 179, Đường số 8, Chi nhánh SH-04', '2026-09-11 16:16:00', 'Connection Provided',
        53, N'DP-SH-04-MAIN-4', -15.20, N'Đã đấu nối riser quang, kiểm tra tốc độ băng thông ổn định đạt chỉ tiêu gói.',
        'emp-03', '2026-08-24 10:00:00', '2026-08-26', NULL,
        1, 0.00, NULL, 1, NULL,
        'T081-000000000077', N'Aiden Brooks', '2026-08-23 09:00:00', N'Đã hoàn tất duyệt hồ sơ và thanh toán.',
        NULL, NULL, NULL,
        '2026-09-11 16:16:00', '2026-09-11 16:16:00'
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderID = 'T0000000068')
BEGIN
    INSERT INTO dbo.Orders (
        OrderID, CustomerID, PlanID, StoreID, EmployeeID,
        InstallationAddress, ApplicationDate, Status,
        CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes,
        FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason,
        BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible,
        AssignedAccountId, RetailApprovedBy, RetailApprovedDate, RetailApprovalNotes,
        AssignedTechnicianName, AssignedTechnicianPhone, AssignedTechnicianDate,
        CreatedAt, UpdatedAt
    ) VALUES (
        'T0000000068', 'CUST-0015', 'plan-ll-std-y', 'SH-02', 'emp-05',
        N'Địa chỉ lắp đặt: Số 186, Đường số 9, Chi nhánh SH-02', '2026-09-16 08:23:00', 'Not Feasible',
        1250, N'No Spare Ports / OLT Saturated', -35.50, N'Khoảng cách từ trạm DP tới địa chỉ lắp đặt vượt quá 1.2km, suy hao quang đo được -35.5 dBm vượt ngưỡng quy định.',
        'emp-03', '2026-09-16 16:30:00', NULL, N'Khoảng cách cáp > 1.2km và suy hao tín hiệu quang vượt ngưỡng cho phép (-35.5 dBm). Cần mở rộng trạm lặp.',
        1, 0.00, NULL, 0, 0,
        NULL, N'David Chen', '2026-09-15 09:30:00', N'Hồ sơ hợp lệ, đã duyệt chuyển khảo sát.',
        NULL, NULL, NULL,
        '2026-09-16 08:23:00', '2026-09-16 08:23:00'
    );
END;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT '3. Thêm tài khoản kết nối cho các đơn đã cấp kết nối vào dbo.CustomerConnection...';

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B064-000000000013')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B064-000000000013', 'B0000000022', NULL, 'Active', '103.21.23.92', 'PORT-SH-01-14', '2026-08-24 14:00:00', '2026-08-24 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B072-000000000014')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B072-000000000014', 'B0000000023', NULL, 'Active', '103.21.24.99', 'PORT-SH-03-15', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B081-000000000016')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B081-000000000016', 'B0000000026', NULL, 'Active', '103.21.26.113', 'PORT-SH-04-1', '2026-08-19 14:00:00', '2026-08-19 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B064-000000000017')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B064-000000000017', 'B0000000027', NULL, 'Active', '103.21.27.120', 'PORT-SH-02-2', '2026-08-20 14:00:00', '2026-08-20 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B064-000000000020')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B064-000000000020', 'B0000000035', NULL, 'Active', '103.21.30.141', 'PORT-SH-02-5', '2026-08-24 14:00:00', '2026-08-24 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B081-000000000021')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B081-000000000021', 'B0000000036', NULL, 'Active', '103.21.31.148', 'PORT-SH-04-6', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B072-000000000024')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B072-000000000024', 'B0000000044', NULL, 'Active', '103.21.34.169', 'PORT-SH-03-9', '2026-08-24 14:00:00', '2026-08-24 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B064-000000000025')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B064-000000000025', 'B0000000045', NULL, 'Active', '103.21.35.176', 'PORT-SH-01-10', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B064-000000000028')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B064-000000000028', 'B0000000054', NULL, 'Active', '103.21.38.197', 'PORT-SH-02-13', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'B081-000000000029')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('B081-000000000029', 'B0000000055', NULL, 'Active', '103.21.39.204', 'PORT-SH-04-14', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D064-000000000032')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D064-000000000032', 'D0000000014', NULL, 'Active', '192.168.32.97', 'PORT-SH-02-1', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D081-000000000033')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D081-000000000033', 'D0000000015', NULL, 'Active', '192.168.33.100', 'PORT-SH-04-2', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D081-000000000036')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D081-000000000036', 'D0000000024', NULL, 'Active', '192.168.36.109', 'PORT-SH-04-5', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D064-000000000037')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D064-000000000037', 'D0000000025', NULL, 'Active', '192.168.37.112', 'PORT-SH-02-6', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D064-000000000040')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D064-000000000040', 'D0000000032', NULL, 'Active', '192.168.40.121', 'PORT-SH-02-9', '2026-08-23 14:00:00', '2026-08-23 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D081-000000000041')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D081-000000000041', 'D0000000033', NULL, 'Active', '192.168.41.124', 'PORT-SH-04-10', '2026-08-24 14:00:00', '2026-08-24 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D064-000000000044')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D064-000000000044', 'D0000000042', NULL, 'Active', '192.168.44.133', 'PORT-SH-02-13', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D081-000000000045')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D081-000000000045', 'D0000000043', NULL, 'Active', '192.168.45.136', 'PORT-SH-04-14', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D081-000000000048')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D081-000000000048', 'D0000000052', NULL, 'Active', '192.168.48.145', 'PORT-SH-04-1', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D064-000000000049')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D064-000000000049', 'D0000000053', NULL, 'Active', '192.168.49.148', 'PORT-SH-02-2', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D064-000000000052')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D064-000000000052', 'D0000000062', NULL, 'Active', '192.168.52.157', 'PORT-SH-02-5', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'D081-000000000053')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('D081-000000000053', 'D0000000063', NULL, 'Active', '192.168.53.160', 'PORT-SH-04-6', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T064-000000000056')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T064-000000000056', 'T0000000018', NULL, 'Active', '198.51.100.57', 'PORT-SH-01-9', '2026-08-24 14:00:00', '2026-08-24 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T072-000000000057')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T072-000000000057', 'T0000000019', NULL, 'Active', '198.51.100.58', 'PORT-SH-03-10', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T064-000000000060')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T064-000000000060', 'T0000000027', NULL, 'Active', '198.51.100.61', 'PORT-SH-02-13', '2026-08-24 14:00:00', '2026-08-24 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T081-000000000061')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T081-000000000061', 'T0000000028', NULL, 'Active', '198.51.100.62', 'PORT-SH-04-14', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T064-000000000064')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T064-000000000064', 'T0000000037', NULL, 'Active', '198.51.100.65', 'PORT-SH-01-1', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T072-000000000065')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T072-000000000065', 'T0000000038', NULL, 'Active', '198.51.100.66', 'PORT-SH-03-2', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T064-000000000068')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T064-000000000068', 'T0000000046', NULL, 'Active', '198.51.100.69', 'PORT-SH-01-5', '2026-08-24 14:00:00', '2026-08-24 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T072-000000000069')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T072-000000000069', 'T0000000047', NULL, 'Active', '198.51.100.70', 'PORT-SH-03-6', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T081-000000000072')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T081-000000000072', 'T0000000056', NULL, 'Active', '198.51.100.73', 'PORT-SH-04-9', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T064-000000000073')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T064-000000000073', 'T0000000057', NULL, 'Active', '198.51.100.74', 'PORT-SH-02-10', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T064-000000000076')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T064-000000000076', 'T0000000066', NULL, 'Active', '198.51.100.77', 'PORT-SH-02-13', '2026-08-25 14:00:00', '2026-08-25 15:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerConnection WHERE AccountID = 'T081-000000000077')
BEGIN
    INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, LastUpdated)
    VALUES ('T081-000000000077', 'T0000000067', NULL, 'Active', '198.51.100.78', 'PORT-SH-04-14', '2026-08-26 14:00:00', '2026-08-26 15:00:00');
END;
GO

PRINT '4. Kiểm tra số lượng đơn hàng theo từng gói cước trong dbo.Orders:';
SELECT 
    sp.PlanID,
    sp.ConnectionType,
    sp.Name AS PlanName,
    COUNT(o.OrderID) AS OrderCount
FROM dbo.ServicePlan sp
LEFT JOIN dbo.Orders o ON sp.PlanID = o.PlanID
GROUP BY sp.PlanID, sp.ConnectionType, sp.Name
ORDER BY sp.ConnectionType, sp.PlanID;
GO
