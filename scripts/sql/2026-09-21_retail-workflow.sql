/* ===========================================================================
   PATCH 2026-09-21 — Hoàn thiện khâu Cửa hàng bán lẻ (Retail Stage)
   ---------------------------------------------------------------------------
   Vì sao cần patch này:
   - Frontend dùng 6 trạng thái đơn hàng (OrderStatus trong src/types/nexus.ts)
     nhưng CK_Orders_Status trong DB chỉ cho phép 4. Hai trạng thái của khâu bán lẻ
     là 'PendingRetail' và 'Not Approved' bị CSDL chặn, nên thao tác duyệt/từ chối
     đơn không thể lưu xuống DB.
   - Bảng Orders chưa có cột lưu vết ai duyệt đơn và ai được phân công khảo sát,
     nên các thông tin này mất sạch sau khi tải lại trang.

   Script viết theo kiểu idempotent — chạy lại nhiều lần vẫn an toàn.
   Cách chạy:
     sqlcmd -S "(localdb)\MSSQLLocalDB" -d NexusSystem -i 2026-09-21_retail-workflow.sql
   =========================================================================== */

USE NexusSystem;
GO

/* ---------------------------------------------------------------------------
   1. Nới ràng buộc trạng thái đơn hàng cho đủ 6 giá trị frontend đang dùng.
      Nới rộng CHECK không làm hỏng dữ liệu cũ vì 4 giá trị cũ vẫn nằm trong tập mới.
   --------------------------------------------------------------------------- */
IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Orders_Status' AND parent_object_id = OBJECT_ID('dbo.Orders')
)
    ALTER TABLE dbo.Orders DROP CONSTRAINT CK_Orders_Status;
GO

ALTER TABLE dbo.Orders WITH CHECK ADD CONSTRAINT CK_Orders_Status CHECK (
    Status IN (
        'PendingRetail',        -- vừa nộp, đang chờ cửa hàng bán lẻ duyệt hồ sơ
        'Not Approved',         -- cửa hàng bán lẻ từ chối
        'Pending',              -- đã qua bán lẻ, đang chờ kỹ thuật khảo sát
        'Feasible',
        'Not Feasible',
        'Connection Provided'
    )
);
GO

/* ---------------------------------------------------------------------------
   2. Cột lưu vết duyệt hồ sơ tại cửa hàng bán lẻ.
   --------------------------------------------------------------------------- */
IF COL_LENGTH('dbo.Orders', 'RetailApprovedBy') IS NULL
    ALTER TABLE dbo.Orders ADD RetailApprovedBy NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'RetailApprovedDate') IS NULL
    ALTER TABLE dbo.Orders ADD RetailApprovedDate DATETIME NULL;
GO

IF COL_LENGTH('dbo.Orders', 'RetailApprovalNotes') IS NULL
    ALTER TABLE dbo.Orders ADD RetailApprovalNotes NVARCHAR(500) NULL;
GO

/* ---------------------------------------------------------------------------
   3. Cột lưu vết phân công kỹ thuật viên đi khảo sát.
      EmployeeID sẵn có chỉ giữ mã nhân viên; tên và số điện thoại lưu thêm ở đây
      để giao diện hiện đúng người đã được cử đi mà không phải join lại.
   --------------------------------------------------------------------------- */
IF COL_LENGTH('dbo.Orders', 'AssignedTechnicianName') IS NULL
    ALTER TABLE dbo.Orders ADD AssignedTechnicianName NVARCHAR(100) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'AssignedTechnicianPhone') IS NULL
    ALTER TABLE dbo.Orders ADD AssignedTechnicianPhone VARCHAR(20) NULL;
GO

IF COL_LENGTH('dbo.Orders', 'AssignedTechnicianDate') IS NULL
    ALTER TABLE dbo.Orders ADD AssignedTechnicianDate DATETIME NULL;
GO

/* ---------------------------------------------------------------------------
   4. Index phục vụ hàng đợi của cửa hàng bán lẻ (lọc đơn theo cửa hàng + trạng thái).
   --------------------------------------------------------------------------- */
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Orders_Store_Status' AND object_id = OBJECT_ID('dbo.Orders')
)
    CREATE INDEX IX_Orders_Store_Status ON dbo.Orders (StoreID, Status);
GO

PRINT 'Patch 2026-09-21 (retail workflow) da duoc ap dung.';
GO
