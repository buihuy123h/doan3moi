-- ===================================================================================
-- NEXUS SYSTEM - PATCH: Equipment Import Requests from Technicians to Admin
-- Table: dbo.EquipmentRequest
-- Date: 2026-09-28
-- ===================================================================================

USE NexusSystem;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.EquipmentRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EquipmentRequest (
        RequestID              VARCHAR(30)      NOT NULL, -- e.g. 'REQ-20260928-0001'
        OrderID                VARCHAR(11)      NULL,     -- Link to customer order if triggered from order
        StoreID                VARCHAR(20)      NOT NULL, -- RetailStore requesting stock
        EmployeeID             VARCHAR(20)      NOT NULL, -- Technician / requester
        InventoryID            VARCHAR(30)      NULL,     -- Link to stock catalog model
        ItemName               NVARCHAR(150)    NOT NULL, -- e.g. 'AX-100 Fiber Modem'
        DeviceType             VARCHAR(50)      NOT NULL, -- 'Modem', 'Router', 'Fiber ONT Modem', etc.
        Quantity               INT              NOT NULL DEFAULT 1 CHECK (Quantity >= 1),
        Urgency                VARCHAR(20)      NOT NULL DEFAULT 'Normal' CHECK (Urgency IN ('Low', 'Normal', 'High', 'Urgent')),
        Status                 VARCHAR(30)      NOT NULL DEFAULT 'Pending' CHECK (Status IN ('Pending', 'Approved', 'Fulfilled', 'Rejected')),
        Reason                 NVARCHAR(500)    NULL,
        AdminNotes             NVARCHAR(500)    NULL,
        CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
        UpdatedAt              DATETIME         NULL,

        CONSTRAINT PK_EquipmentRequest PRIMARY KEY CLUSTERED (RequestID),
        CONSTRAINT FK_EquipmentRequest_Orders FOREIGN KEY (OrderID) REFERENCES dbo.Orders(OrderID) ON DELETE SET NULL,
        CONSTRAINT FK_EquipmentRequest_RetailStore FOREIGN KEY (StoreID) REFERENCES dbo.RetailStore(StoreID),
        CONSTRAINT FK_EquipmentRequest_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.Employee(EmployeeID),
        CONSTRAINT FK_EquipmentRequest_Inventory FOREIGN KEY (InventoryID) REFERENCES dbo.InventoryItem(InventoryID) ON DELETE SET NULL
    );

    CREATE NONCLUSTERED INDEX IX_EquipmentRequest_Status ON dbo.EquipmentRequest(Status);
    CREATE NONCLUSTERED INDEX IX_EquipmentRequest_StoreID ON dbo.EquipmentRequest(StoreID);
    CREATE NONCLUSTERED INDEX IX_EquipmentRequest_EmployeeID ON dbo.EquipmentRequest(EmployeeID);
    CREATE NONCLUSTERED INDEX IX_EquipmentRequest_OrderID ON dbo.EquipmentRequest(OrderID);

    PRINT '>> Created dbo.EquipmentRequest table successfully.';
END
ELSE
BEGIN
    PRINT '>> dbo.EquipmentRequest table already exists.';
END
GO

-- Seed sample data if empty
IF NOT EXISTS (SELECT 1 FROM dbo.EquipmentRequest)
BEGIN
    INSERT INTO dbo.EquipmentRequest (RequestID, OrderID, StoreID, EmployeeID, InventoryID, ItemName, DeviceType, Quantity, Urgency, Status, Reason, AdminNotes, CreatedAt)
    VALUES
    ('REQ-20260928-0001', 'B0000000002', 'SH-01', 'emp-03', 'inv-01', N'Huawei EchoLife HG8245H5 GPON ONT', 'Fiber ONT Modem', 5, 'High', 'Pending', N'Kho chi nhánh hết thiết bị Fiber ONT để cấp kết nối cho đơn hàng mới.', NULL, DATEADD(HOUR, -4, GETDATE())),
    ('REQ-20260928-0002', NULL, 'SH-02', 'emp-03', 'inv-02', N'Nexus Wi-Fi 6 AX3000 Dual-Band Router', 'Gigabit Router', 10, 'Normal', 'Approved', N'Bổ sung tồn kho dự phòng Router Wi-Fi 6 cho chi nhánh SH-02.', N'Đã duyệt, chuẩn bị điều phối hàng về kho.', DATEADD(DAY, -1, GETDATE()));

    PRINT '>> Seeded sample records into dbo.EquipmentRequest.';
END
GO
