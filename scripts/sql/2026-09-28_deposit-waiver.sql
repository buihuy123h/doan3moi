/* ===========================================================================
   PATCH 2026-09-28 — Miễn tiền cọc cho đơn số lượng lớn (> 50 kết nối)
   ---------------------------------------------------------------------------
   Theo đề bài (bulk scheme): "if it is above 50 they will avail 100% discount"
   trên tiền ứng trước VÀ tiền cọc. Trước đây mức 100% chỉ được tính ngầm trong
   BulkDiscountPercent, CSDL không có chỗ nào ghi rõ "đơn này được miễn cọc".

   DepositWaived là CỘT TÍNH TOÁN (computed column): SQL Server tự tính theo
   BulkConnectionsCount, nên không bao giờ lệch với số lượng thật và không cần
   UPDATE dữ liệu cũ — các đơn > 50 kết nối có sẵn tự nhận cờ = 1.
   Cố ý KHÔNG dùng PERSISTED: cột lưu cứng đòi QUOTED_IDENTIFIER ON cho mọi lệnh ghi,
   trong khi sqlcmd mặc định tắt — chạy lại file seed bằng sqlcmd sẽ bị lỗi.

   Hoá đơn của đơn miễn cọc được lập với Bill.SecurityDeposit = 0
   (xem BillingService.CreateBillAsync).

   Script idempotent — chạy lại nhiều lần vẫn an toàn.
   Cách chạy:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -d NexusSystem -i 2026-09-28_deposit-waiver.sql
   =========================================================================== */

USE NexusSystem;
GO

IF COL_LENGTH('dbo.Orders', 'DepositWaived') IS NULL
    ALTER TABLE dbo.Orders ADD DepositWaived AS
        CAST(CASE WHEN BulkConnectionsCount > 50 THEN 1 ELSE 0 END AS BIT);
GO

PRINT 'Patch 2026-09-28 (mien tien coc > 50 ket noi) da duoc ap dung.';
GO
