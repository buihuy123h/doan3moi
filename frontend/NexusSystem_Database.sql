-- ===================================================================================
-- NEXUS TELECOM SERVICE MARKETING SYSTEM - ENHANCED DATABASE ARCHITECTURE
-- Database Name: NexusSystem
-- Standardization: 3NF (Third Normal Form) & Zero-Redundancy Architecture
-- Compatibility: Microsoft SQL Server 2016+ / Azure SQL / EF Core / Svelte 5 Frontend
-- ===================================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'NexusSystem')
BEGIN
    CREATE DATABASE NexusSystem
    COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE NexusSystem;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ===================================================================================
-- 1. DROP EXISTING TABLES IN REVERSE DEPENDENCY ORDER (SAFE RE-RUN)
-- ===================================================================================
IF OBJECT_ID(N'dbo.ConnectionActivityLog', N'U') IS NOT NULL DROP TABLE dbo.ConnectionActivityLog;
IF OBJECT_ID(N'dbo.Feedback', N'U') IS NOT NULL DROP TABLE dbo.Feedback;
IF OBJECT_ID(N'dbo.Payment', N'U') IS NOT NULL DROP TABLE dbo.Payment;
IF OBJECT_ID(N'dbo.Bill', N'U') IS NOT NULL DROP TABLE dbo.Bill;
IF OBJECT_ID(N'dbo.CustomerConnection', N'U') IS NOT NULL DROP TABLE dbo.CustomerConnection;
IF OBJECT_ID(N'dbo.Equipment', N'U') IS NOT NULL DROP TABLE dbo.Equipment;
IF OBJECT_ID(N'dbo.Orders', N'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID(N'dbo.Customer', N'U') IS NOT NULL DROP TABLE dbo.Customer;
IF OBJECT_ID(N'dbo.InventoryItem', N'U') IS NOT NULL DROP TABLE dbo.InventoryItem;
IF OBJECT_ID(N'dbo.ServicePlan', N'U') IS NOT NULL DROP TABLE dbo.ServicePlan;
IF OBJECT_ID(N'dbo.Employee', N'U') IS NOT NULL DROP TABLE dbo.Employee;
IF OBJECT_ID(N'dbo.RetailStore', N'U') IS NOT NULL DROP TABLE dbo.RetailStore;
IF OBJECT_ID(N'dbo.Vendor', N'U') IS NOT NULL DROP TABLE dbo.Vendor;
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL DROP TABLE dbo.SystemSettings;
GO

-- ===================================================================================
-- 2. CREATE REFINED 3NF TABLES (ZERO DUPLICATION, FULL FRONTEND COMPATIBILITY)
-- ===================================================================================

-- 2.1. RETAIL STORE TABLE (Cửa hàng bán lẻ)
CREATE TABLE dbo.RetailStore (
    StoreID                VARCHAR(20)      NOT NULL, -- e.g., 'SH-01'
    Name                   NVARCHAR(150)    NOT NULL,
    City                   NVARCHAR(100)    NOT NULL,
    CityCode               CHAR(3)          NOT NULL, -- 3-digit city code in 16-char AccountID ('064', '072', '081')
    Address                NVARCHAR(255)    NOT NULL,
    ManagerName            NVARCHAR(100)    NULL,
    Phone                  VARCHAR(30)      NULL,
    Email                  VARCHAR(100)     NULL,
    OperatingHours         NVARCHAR(100)    NULL,
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Active',
    ActiveEmployeesCount   INT              NOT NULL DEFAULT 0,
    TotalSubscribersServed INT              NOT NULL DEFAULT 0,
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_RetailStore PRIMARY KEY CLUSTERED (StoreID),
    CONSTRAINT CK_RetailStore_CityCode CHECK (LEN(CityCode) = 3 AND CityCode NOT LIKE '%[^0-9]%'),
    CONSTRAINT CK_RetailStore_Status CHECK (Status IN ('Active', 'Renovating', 'Closed'))
);
GO

-- 2.2. EMPLOYEE TABLE (Nhân viên)
CREATE TABLE dbo.Employee (
    EmployeeID             VARCHAR(20)      NOT NULL, -- e.g., 'emp-01'
    EmployeeCode           VARCHAR(20)      NOT NULL, -- e.g., 'EMP-1001'
    FullName               NVARCHAR(100)    NOT NULL,
    Email                  VARCHAR(100)     NOT NULL,
    Phone                  VARCHAR(30)      NULL,
    PasswordHash           NVARCHAR(255)    NOT NULL,
    Role                   VARCHAR(50)      NOT NULL, -- 'Manager', 'Retail Staff', 'Field Engineer', 'Senior Accountant', 'Support Agent'
    Department             NVARCHAR(100)    NOT NULL, -- 'Administration', 'Retail Outlets', 'Technical Operations', 'Finance & Accounts'
    StoreID                VARCHAR(20)      NULL,     -- Assigned Retail Shop FK
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Active',
    DateOfJoining          DATE             NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    AvatarUrl              NVARCHAR(500)    NULL,
    Address                NVARCHAR(255)    NULL,
    Gender                 NVARCHAR(20)     NULL,
    DateOfBirth            DATE             NULL,
    Bio                    NVARCHAR(500)    NULL,
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_Employee PRIMARY KEY CLUSTERED (EmployeeID),
    CONSTRAINT UQ_Employee_Email UNIQUE (Email),
    CONSTRAINT UQ_Employee_Code UNIQUE (EmployeeCode),
    CONSTRAINT FK_Employee_RetailStore FOREIGN KEY (StoreID) REFERENCES dbo.RetailStore(StoreID) ON DELETE SET NULL,
    CONSTRAINT CK_Employee_Status CHECK (Status IN ('Active', 'Inactive', 'Suspended'))
);
GO

-- 2.3. VENDOR TABLE (Nhà cung cấp thiết bị viễn thông)
CREATE TABLE dbo.Vendor (
    VendorID               VARCHAR(20)      NOT NULL, -- e.g., 'vnd-01'
    VendorCode             VARCHAR(20)      NOT NULL, -- e.g., 'VND-401'
    CompanyName            NVARCHAR(150)    NOT NULL,
    ContactPerson          NVARCHAR(100)    NULL,
    Category               NVARCHAR(100)    NOT NULL, -- 'Fiber Optics & Cabling', 'Modems & Routers', 'Telecom Switches', 'Field Tooling'
    Phone                  VARCHAR(30)      NULL,
    Email                  VARCHAR(100)     NULL,
    Address                NVARCHAR(255)    NULL,
    TaxNumber              VARCHAR(50)      NULL,
    Rating                 TINYINT          NOT NULL DEFAULT 5,
    Status                 VARCHAR(30)      NOT NULL DEFAULT 'Active',
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_Vendor PRIMARY KEY CLUSTERED (VendorID),
    CONSTRAINT UQ_Vendor_Code UNIQUE (VendorCode),
    CONSTRAINT CK_Vendor_Rating CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT CK_Vendor_Status CHECK (Status IN ('Active', 'Pending Review', 'Terminated'))
);
GO

-- 2.4. SERVICE PLAN TABLE (Gói cước viễn thông)
CREATE TABLE dbo.ServicePlan (
    PlanID                 VARCHAR(30)      NOT NULL, -- e.g., 'plan-bb-64', 'plan-du-56', 'plan-ll-std-m'
    PlanCode               VARCHAR(30)      NULL,     -- Commercial tariff code e.g., 'BB-U64'
    Name                   NVARCHAR(150)    NOT NULL,
    ConnectionType         VARCHAR(20)      NOT NULL, -- 'Broadband', 'Dial-Up', 'Landline'
    SpeedOrBandwidth       VARCHAR(100)     NOT NULL, -- e.g., '56 Kbps', '128 Kbps', 'PSTN Voice'
    MonthlyRental          DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    HourlyCharge           DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    SecurityDeposit        DECIMAL(18, 2)   NOT NULL DEFAULT 0.00, -- 500 Broadband / 325 Dial-Up / 250 Landline
    DataLimit              VARCHAR(50)      NULL,     -- 'Unlimited', '10 Hours', '30 Hours', 'Local Calling'
    BillingCycle           VARCHAR(30)      NOT NULL DEFAULT 'Monthly', -- 'Hourly Pack', 'Monthly', 'Quarterly', 'Half-Yearly', 'Yearly'
    Validity               VARCHAR(50)      NULL,     -- '1 Month', '3 Months', '6 Months', '1 Year'
    IncludedHours          INT              NOT NULL DEFAULT 0,
    CallRates              NVARCHAR(255)    NULL,
    Description            NVARCHAR(500)    NULL,
    IsPopular              BIT              NOT NULL DEFAULT 0,
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Active',
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_ServicePlan PRIMARY KEY CLUSTERED (PlanID),
    CONSTRAINT CK_ServicePlan_ConnectionType CHECK (ConnectionType IN ('Broadband', 'Dial-Up', 'Landline')),
    CONSTRAINT CK_ServicePlan_Status CHECK (Status IN ('Active', 'Archived'))
);
GO

-- 2.5. INVENTORY ITEM TABLE (Quản lý kho vật tư - Phục vụ trực tiếp Tab Stock của Admin)
CREATE TABLE dbo.InventoryItem (
    InventoryID            VARCHAR(30)      NOT NULL, -- e.g., 'inv-01'
    ItemCode               VARCHAR(50)      NOT NULL, -- e.g., 'EQ-ONT-FBR', 'EQ-RTR-AX'
    Name                   NVARCHAR(150)    NOT NULL,
    Category               VARCHAR(50)      NOT NULL, -- 'Modem', 'Router', 'Fiber ONT', 'Splitter', 'Patch Cord', 'VoIP Adapter'
    StockQuantity          INT              NOT NULL DEFAULT 0,
    ReorderLevel           INT              NOT NULL DEFAULT 10,
    UnitCost               DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    Location               NVARCHAR(100)    NULL,     -- e.g., 'Central Depot Bay 4A'
    VendorID               VARCHAR(20)      NULL,     -- Supplier link
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'In Stock',
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_InventoryItem PRIMARY KEY CLUSTERED (InventoryID),
    CONSTRAINT UQ_InventoryItem_Code UNIQUE (ItemCode),
    CONSTRAINT FK_InventoryItem_Vendor FOREIGN KEY (VendorID) REFERENCES dbo.Vendor(VendorID) ON DELETE SET NULL,
    CONSTRAINT CK_InventoryItem_Stock CHECK (StockQuantity >= 0)
);
GO

-- 2.6. CUSTOMER TABLE (Khách hàng)
CREATE TABLE dbo.Customer (
    CustomerID             VARCHAR(20)      NOT NULL, -- e.g., 'CUST-0001'
    FullName               NVARCHAR(100)    NOT NULL,
    CustomerType           VARCHAR(20)      NOT NULL DEFAULT 'Individual', -- 'Individual' vs 'Corporate' (Bulk scheme)
    Email                  VARCHAR(100)     NULL,
    ContactNumber          VARCHAR(30)      NOT NULL,
    Address                NVARCHAR(255)    NOT NULL,
    BillingAddress         NVARCHAR(255)    NULL,     -- Separate billing location for corporate accounts
    TaxNumber              VARCHAR(50)      NULL,     -- Tax registration number for B2B accounts
    IdProofType            VARCHAR(50)      NOT NULL DEFAULT 'National ID Card',
    IdProofNumber          VARCHAR(50)      NOT NULL,
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Active',
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED (CustomerID),
    CONSTRAINT CK_Customer_Type CHECK (CustomerType IN ('Individual', 'Corporate')),
    CONSTRAINT CK_Customer_IdProofType CHECK (IdProofType IN ('National ID Card', 'Passport', 'Driver''s License')),
    CONSTRAINT CK_Customer_Status CHECK (Status IN ('Active', 'Suspended', 'Terminated'))
);
GO

-- 2.7. ORDERS TABLE (Đơn hàng & Khảo sát đo kiểm)
CREATE TABLE dbo.Orders (
    OrderID                VARCHAR(11)      NOT NULL, -- Exactly 11 chars: [D|B|T] + 10 digits
    CustomerID             VARCHAR(20)      NOT NULL,
    PlanID                 VARCHAR(30)      NOT NULL,
    StoreID                VARCHAR(20)      NOT NULL,
    EmployeeID             VARCHAR(20)      NULL,     -- Retail sales executive who registered order
    InstallationAddress    NVARCHAR(255)    NOT NULL,
    ApplicationDate        DATETIME         NOT NULL DEFAULT GETDATE(),
    Status                 VARCHAR(30)      NOT NULL DEFAULT 'Pending',

    -- Technical Feasibility Metrics
    CableDistanceMeters    INT              NULL,
    DpBoxCapacity          NVARCHAR(100)    NULL,
    SignalLossDbm          DECIMAL(5, 2)    NULL,
    FeasibilityNotes       NVARCHAR(500)    NULL,
    FeasibilityCheckedBy   VARCHAR(20)      NULL,     -- Field engineer who verified line
    FeasibilityCheckedDate DATETIME         NULL,
    ScheduledInstallDate   DATETIME         NULL,
    RejectionReason        NVARCHAR(500)    NULL,

    -- Khâu Cửa hàng bán lẻ (Retail Stage): lưu vết ai duyệt hồ sơ và ghi chú kèm theo
    RetailApprovedBy       NVARCHAR(100)    NULL,
    RetailApprovedDate     DATETIME         NULL,
    RetailApprovalNotes    NVARCHAR(500)    NULL,

    -- Phân công kỹ thuật viên đi khảo sát (EmployeeID chỉ giữ mã, 3 cột này giữ thông tin hiển thị)
    AssignedTechnicianName  NVARCHAR(100)   NULL,
    AssignedTechnicianPhone VARCHAR(20)     NULL,
    AssignedTechnicianDate  DATETIME        NULL,

    -- Bulk / Corporate Scheme Discounts (Mặc định 50 connection / router thiết bị per Phương án 1)
    BulkConnectionsCount   INT              NOT NULL DEFAULT 50,
    BulkDiscountPercent    DECIMAL(5, 2)    NOT NULL DEFAULT 0.00,

    -- Dial-Up Dual-Leg Feasibility Flags
    ExistingLandlineAccountId VARCHAR(20)   NULL,
    LandlineFeasible       BIT              NULL,
    InternetFeasible       BIT              NULL,
    AssignedAccountId      VARCHAR(20)      NULL,
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (OrderID),
    CONSTRAINT FK_Orders_Customer FOREIGN KEY (CustomerID) REFERENCES dbo.Customer(CustomerID),
    CONSTRAINT FK_Orders_ServicePlan FOREIGN KEY (PlanID) REFERENCES dbo.ServicePlan(PlanID),
    CONSTRAINT FK_Orders_RetailStore FOREIGN KEY (StoreID) REFERENCES dbo.RetailStore(StoreID),
    CONSTRAINT FK_Orders_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.Employee(EmployeeID) ON DELETE SET NULL,
    CONSTRAINT FK_Orders_FeasibilityEngineer FOREIGN KEY (FeasibilityCheckedBy) REFERENCES dbo.Employee(EmployeeID),
    CONSTRAINT CK_Orders_ID_Format CHECK (LEN(OrderID) = 11 AND OrderID LIKE '[DBT][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    -- 6 trạng thái khớp với type OrderStatus bên frontend (src/types/nexus.ts):
    -- PendingRetail -> chờ cửa hàng bán lẻ duyệt; Not Approved -> bán lẻ từ chối;
    -- Pending -> đã chuyển sang hàng đợi khảo sát của Kỹ thuật.
    CONSTRAINT CK_Orders_Status CHECK (Status IN ('PendingRetail', 'Not Approved', 'Pending', 'Feasible', 'Not Feasible', 'Connection Provided')),
    CONSTRAINT CK_Orders_BulkDiscount CHECK (BulkDiscountPercent >= 0 AND BulkDiscountPercent <= 100)
);
GO

-- 2.8. EQUIPMENT TABLE (Thiết bị định danh đã gán kết nối hoặc CPE sẵn sàng triển khai)
CREATE TABLE dbo.Equipment (
    EquipmentID            VARCHAR(20)      NOT NULL, -- e.g., 'eq-01'
    InventoryID            VARCHAR(30)      NULL,     -- Link to stock catalog model
    SerialNumber           VARCHAR(50)      NULL,
    MacAddress             VARCHAR(50)      NULL,
    DeviceModel            NVARCHAR(150)    NOT NULL,
    DeviceType             VARCHAR(50)      NOT NULL, -- 'Fiber ONT Modem', 'VDSL2/ADSL Modem', 'Gigabit Router', 'Analog Telephone Adapter'
    VendorID               VARCHAR(20)      NOT NULL,
    StoreID                VARCHAR(20)      NULL,     -- Assigned depot/store
    Status                 VARCHAR(30)      NOT NULL DEFAULT 'In Stock', -- 'In Service', 'In Stock', 'Maintenance', 'Faulty'
    FirmwareVersion        VARCHAR(50)      NULL,
    AssignedAccountId      VARCHAR(20)      NULL,
    AssignedTechnicianId   VARCHAR(20)      NULL,     -- Field engineer assigned
    InstalledDate          DATETIME         NULL,
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,

    CONSTRAINT PK_Equipment PRIMARY KEY CLUSTERED (EquipmentID),
    CONSTRAINT UQ_Equipment_SerialNumber UNIQUE (SerialNumber),
    CONSTRAINT FK_Equipment_Inventory FOREIGN KEY (InventoryID) REFERENCES dbo.InventoryItem(InventoryID) ON DELETE SET NULL,
    CONSTRAINT FK_Equipment_Vendor FOREIGN KEY (VendorID) REFERENCES dbo.Vendor(VendorID),
    CONSTRAINT FK_Equipment_RetailStore FOREIGN KEY (StoreID) REFERENCES dbo.RetailStore(StoreID) ON DELETE SET NULL,
    CONSTRAINT FK_Equipment_Employee FOREIGN KEY (AssignedTechnicianId) REFERENCES dbo.Employee(EmployeeID) ON DELETE SET NULL,
    CONSTRAINT CK_Equipment_Status CHECK (Status IN ('In Stock', 'In Service', 'Maintenance', 'Faulty'))
);
GO

-- 2.9. CUSTOMER CONNECTION TABLE (Đường truyền thuê bao viễn thông)
CREATE TABLE dbo.CustomerConnection (
    AccountID              VARCHAR(20)      NOT NULL, -- 16-20 char canonical account identifier
    OrderID                VARCHAR(11)      NOT NULL, -- Phương án 1: 1 Order -> N Connections (cho phép nhiều connection trên 1 đơn hàng)
    EquipmentID            VARCHAR(20)      NULL,     -- 1 Connection = 1 Router thiết bị duy nhất (ràng buộc qua UQ_CustomerConnection_EquipmentID)
    Status                 VARCHAR(30)      NOT NULL DEFAULT 'Active',
    IpAddress              VARCHAR(50)      NULL,
    PortNumber             VARCHAR(50)      NULL,
    InstalledDate          DATETIME         NOT NULL DEFAULT GETDATE(),
    SuspensionStartDate    DATETIME         NULL,
    SuspensionEndDate      DATETIME         NULL,
    TerminatedDate         DATETIME         NULL,
    LastUpdated            DATETIME         NOT NULL DEFAULT GETDATE(),
    LastStatusReason       NVARCHAR(500)    NULL,

    CONSTRAINT PK_CustomerConnection PRIMARY KEY CLUSTERED (AccountID),
    CONSTRAINT FK_CustomerConnection_Order FOREIGN KEY (OrderID) REFERENCES dbo.Orders(OrderID),
    CONSTRAINT FK_CustomerConnection_Equipment FOREIGN KEY (EquipmentID) REFERENCES dbo.Equipment(EquipmentID) ON DELETE SET NULL,
    CONSTRAINT CK_CustomerConnection_Status CHECK (Status IN ('Active', 'Temporarily Inactive', 'Permanently Inactive'))
);
GO

-- 2.10. BILL / INVOICE TABLE (Hóa đơn cước)
CREATE TABLE dbo.Bill (
    BillID                 VARCHAR(30)      NOT NULL, -- e.g., 'bill-01'
    InvoiceNumber          VARCHAR(50)      NOT NULL, -- e.g., 'NEX-INV-2026-001'
    AccountID              VARCHAR(20)      NOT NULL,
    BillingMonth           VARCHAR(30)      NOT NULL, -- e.g., 'August 2026'
    BillingPeriodStart     DATE             NULL,
    BillingPeriodEnd       DATE             NULL,
    BillingDate            DATE             NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    DueDate                DATE             NOT NULL,

    -- Financial Breakdown
    SecurityDeposit        DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    MonthlyRental          DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    HourlyCharges          DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    DiscountPercent        DECIMAL(5, 2)    NOT NULL DEFAULT 0.00,
    DiscountAmount         DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    Subtotal               DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    ServiceTaxRate         DECIMAL(5, 2)    NOT NULL DEFAULT 12.24, -- 12.24%
    ServiceTaxAmount       DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    LateFeeAmount          DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    TotalAmount            DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,

    -- Settlement Tracking
    AmountPaid             DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    DueAmount              DECIMAL(18, 2)   NOT NULL DEFAULT 0.00,
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Unpaid',
    GeneratedDate          DATETIME         NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_Bill PRIMARY KEY CLUSTERED (BillID),
    CONSTRAINT UQ_Bill_InvoiceNumber UNIQUE (InvoiceNumber),
    CONSTRAINT FK_Bill_CustomerConnection FOREIGN KEY (AccountID) REFERENCES dbo.CustomerConnection(AccountID),
    CONSTRAINT CK_Bill_Status CHECK (Status IN ('Paid', 'Partially Paid', 'Unpaid'))
);
GO

-- 2.11. PAYMENT TABLE (Biên lai thu cước)
CREATE TABLE dbo.Payment (
    PaymentID              VARCHAR(30)      NOT NULL, -- e.g., 'PAY-89201'
    ReceiptNumber          VARCHAR(50)      NULL,     -- Customer voucher number
    BillID                 VARCHAR(30)      NOT NULL,
    EmployeeID             VARCHAR(20)      NULL,     -- Senior accountant who recorded payment
    AmountPaid             DECIMAL(18, 2)   NOT NULL,
    PaymentDate            DATETIME         NOT NULL DEFAULT GETDATE(),
    PaymentMode            VARCHAR(50)      NOT NULL, -- 'Cash', 'Cheque', 'Credit/Debit Card', 'Bank Transfer/NEFT', 'UPI/Digital Wallet'
    ReferenceNumber        VARCHAR(100)     NULL,
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Completed',
    Notes                  NVARCHAR(255)    NULL,

    CONSTRAINT PK_Payment PRIMARY KEY CLUSTERED (PaymentID),
    CONSTRAINT UQ_Payment_ReceiptNumber UNIQUE (ReceiptNumber),
    CONSTRAINT FK_Payment_Bill FOREIGN KEY (BillID) REFERENCES dbo.Bill(BillID),
    CONSTRAINT FK_Payment_Employee FOREIGN KEY (EmployeeID) REFERENCES dbo.Employee(EmployeeID) ON DELETE SET NULL,
    CONSTRAINT CK_Payment_Mode CHECK (PaymentMode IN ('Cash', 'Cheque', 'Credit/Debit Card', 'Bank Transfer/NEFT', 'UPI/Digital Wallet')),
    CONSTRAINT CK_Payment_Status CHECK (Status IN ('Completed', 'Pending Clearance', 'Failed', 'Refunded'))
);
GO

-- 2.13. PURCHASE ORDER TABLE (Purchase List — đơn đặt mua thiết bị từ vendor)
-- Spec: Admin quản lý "purchasal lists"; công ty "order for the equipments based
-- on the orders received for the connection". Đơn Received tự cộng InventoryItem.
CREATE TABLE dbo.PurchaseOrder (
    PurchaseOrderID        VARCHAR(30)      NOT NULL,
    VendorID               VARCHAR(20)      NOT NULL,
    StoreID                VARCHAR(20)      NULL,
    ItemCode               VARCHAR(50)      NOT NULL,
    ItemName               NVARCHAR(150)    NOT NULL,
    Category               NVARCHAR(50)     NOT NULL DEFAULT N'Modem',
    Quantity               INT              NOT NULL DEFAULT 1 CHECK (Quantity >= 1),
    UnitCost               DECIMAL(18,2)    NOT NULL DEFAULT 0,
    TotalCost              DECIMAL(18,2)    NOT NULL DEFAULT 0,
    OrderDate              DATETIME         NOT NULL DEFAULT GETDATE(),
    ExpectedDate           DATETIME         NULL,
    ReceivedDate           DATETIME         NULL,
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Draft' CHECK (Status IN ('Draft','Submitted','Received','Cancelled')),
    Notes                  NVARCHAR(500)    NULL,
    CreatedBy              NVARCHAR(100)    NULL,
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    UpdatedAt              DATETIME         NULL,
    CONSTRAINT PK_PurchaseOrder PRIMARY KEY (PurchaseOrderID),
    CONSTRAINT FK_PurchaseOrder_Vendor FOREIGN KEY (VendorID) REFERENCES dbo.Vendor(VendorID),
    CONSTRAINT FK_PurchaseOrder_RetailStore FOREIGN KEY (StoreID) REFERENCES dbo.RetailStore(StoreID)
);
CREATE INDEX IX_PurchaseOrder_Status ON dbo.PurchaseOrder(Status);
CREATE INDEX IX_PurchaseOrder_VendorID ON dbo.PurchaseOrder(VendorID);

-- 2.12. CUSTOMER FEEDBACK TABLE (Ý kiến đóng góp & khiếu nại của khách hàng)
CREATE TABLE dbo.Feedback (
    FeedbackID             VARCHAR(30)      NOT NULL, -- e.g., 'fb-01'
    AccountID              VARCHAR(20)      NULL,
    OrderID                VARCHAR(11)      NULL,
    CustomerID             VARCHAR(20)      NULL,
    CustomerName           NVARCHAR(100)    NOT NULL,
    Rating                 TINYINT          NOT NULL DEFAULT 5,
    Category               VARCHAR(50)      NOT NULL, -- 'Service Quality', 'Installation', 'Billing', 'Support', 'Other'
    Message                NVARCHAR(MAX)    NOT NULL,
    Status                 VARCHAR(20)      NOT NULL DEFAULT 'Resolved',
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),
    Response               NVARCHAR(MAX)    NULL,
    RespondedBy            VARCHAR(20)      NULL,     -- Staff who replied
    RespondedAt            DATETIME         NULL,

    CONSTRAINT PK_Feedback PRIMARY KEY CLUSTERED (FeedbackID),
    CONSTRAINT FK_Feedback_CustomerConnection FOREIGN KEY (AccountID) REFERENCES dbo.CustomerConnection(AccountID) ON DELETE SET NULL,
    CONSTRAINT FK_Feedback_Orders FOREIGN KEY (OrderID) REFERENCES dbo.Orders(OrderID) ON DELETE SET NULL,
    CONSTRAINT FK_Feedback_Customer FOREIGN KEY (CustomerID) REFERENCES dbo.Customer(CustomerID) ON DELETE SET NULL,
    CONSTRAINT FK_Feedback_Employee FOREIGN KEY (RespondedBy) REFERENCES dbo.Employee(EmployeeID) ON DELETE SET NULL,
    CONSTRAINT CK_Feedback_Rating CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT CK_Feedback_Category CHECK (Category IN ('Service Quality', 'Installation', 'Billing', 'Support', 'Other')),
    CONSTRAINT CK_Feedback_Status CHECK (Status IN ('New', 'Under Review', 'Resolved', 'Closed'))
);
GO

-- 2.13. LIVE CHAT MESSAGE TABLE (Tin nhắn chat trực tuyến giữa khách trên trang index và Admin)
CREATE TABLE dbo.ChatMessage (
    MessageID              VARCHAR(30)      NOT NULL, -- e.g., 'msg-0001'
    SessionID              VARCHAR(30)      NOT NULL, -- e.g., 'chat-01' (một phiên chat của khách)
    SenderType             VARCHAR(10)      NOT NULL, -- 'guest' | 'admin'
    SenderName             NVARCHAR(100)    NOT NULL,
    Message                NVARCHAR(1000)   NOT NULL,
    IsReadByAdmin          BIT              NOT NULL DEFAULT 0,
    CreatedAt              DATETIME         NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_ChatMessage PRIMARY KEY CLUSTERED (MessageID),
    CONSTRAINT CK_ChatMessage_SenderType CHECK (SenderType IN ('guest', 'admin'))
);
GO

CREATE NONCLUSTERED INDEX IX_ChatMessage_SessionID ON dbo.ChatMessage(SessionID, CreatedAt);
GO

-- 2.13. SYSTEM SETTINGS TABLE (Tham số cấu hình hệ thống)
CREATE TABLE dbo.SystemSettings (
    SettingKey             VARCHAR(50)      NOT NULL,
    SettingValue           NVARCHAR(255)    NOT NULL,
    Category               VARCHAR(50)      NOT NULL DEFAULT 'General',
    DataType               VARCHAR(20)      NOT NULL DEFAULT 'string',
    Description            NVARCHAR(255)    NULL,
    LastUpdated            DATETIME         NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_SystemSettings PRIMARY KEY CLUSTERED (SettingKey)
);
GO

-- 2.14. CONNECTION ACTIVITY LOG (Lịch sử hoạt động thuê bao - Audit Trail)
CREATE TABLE dbo.ConnectionActivityLog (
    LogID                  BIGINT IDENTITY(1,1) NOT NULL,
    AccountID              VARCHAR(20)          NOT NULL,
    ActionType             VARCHAR(50)          NOT NULL, -- 'Provisioning', 'StatusChange', 'PlanUpgrade', 'PlanDowngrade', 'DeviceReplacement', 'Relocation'
    OldValue               NVARCHAR(255)        NULL,
    NewValue               NVARCHAR(255)        NULL,
    Reason                 NVARCHAR(500)        NULL,
    PerformedBy            VARCHAR(20)          NULL,     -- Staff ID
    Timestamp              DATETIME             NOT NULL DEFAULT GETDATE(),

    CONSTRAINT PK_ConnectionActivityLog PRIMARY KEY CLUSTERED (LogID),
    CONSTRAINT FK_ConnectionActivityLog_Account FOREIGN KEY (AccountID) REFERENCES dbo.CustomerConnection(AccountID) ON DELETE CASCADE,
    CONSTRAINT FK_ConnectionActivityLog_Employee FOREIGN KEY (PerformedBy) REFERENCES dbo.Employee(EmployeeID) ON DELETE SET NULL
);
GO

-- ===================================================================================
-- 3. OPTIMIZED PERFORMANCE INDEXES
-- ===================================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE NONCLUSTERED INDEX IX_Orders_CustomerID ON dbo.Orders(CustomerID);
CREATE NONCLUSTERED INDEX IX_Orders_StoreID ON dbo.Orders(StoreID);
CREATE NONCLUSTERED INDEX IX_Orders_Status ON dbo.Orders(Status);
CREATE NONCLUSTERED INDEX IX_CustomerConnection_OrderID ON dbo.CustomerConnection(OrderID);
CREATE NONCLUSTERED INDEX IX_CustomerConnection_Status ON dbo.CustomerConnection(Status);
-- Ràng buộc 1 Connection = 1 Router thiết bị: Mỗi Router thiết bị chỉ thuộc về tối đa 1 Connection đang kích hoạt
CREATE UNIQUE NONCLUSTERED INDEX UQ_CustomerConnection_EquipmentID ON dbo.CustomerConnection(EquipmentID) WHERE EquipmentID IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Bill_AccountID ON dbo.Bill(AccountID);
CREATE NONCLUSTERED INDEX IX_Bill_Status ON dbo.Bill(Status);
CREATE NONCLUSTERED INDEX IX_Bill_DueDate ON dbo.Bill(DueDate);
CREATE NONCLUSTERED INDEX IX_Payment_BillID ON dbo.Payment(BillID);
CREATE NONCLUSTERED INDEX IX_InventoryItem_Category ON dbo.InventoryItem(Category);
CREATE NONCLUSTERED INDEX IX_Equipment_Status ON dbo.Equipment(Status);
CREATE NONCLUSTERED INDEX IX_Equipment_VendorID ON dbo.Equipment(VendorID);
CREATE NONCLUSTERED INDEX IX_Feedback_AccountID ON dbo.Feedback(AccountID);
CREATE NONCLUSTERED INDEX IX_ConnectionActivityLog_AccountID ON dbo.ConnectionActivityLog(AccountID);
GO

-- ===================================================================================
-- 4. DATABASE VIEWS FOR REPORTING & REAL-TIME ANALYTICS
-- ===================================================================================

-- 4.1. Active Subscribers View
CREATE OR ALTER VIEW dbo.vw_ActiveSubscribers
AS
SELECT 
    cc.AccountID,
    cc.OrderID,
    c.CustomerID,
    c.FullName AS CustomerName,
    c.CustomerType,
    c.ContactNumber AS CustomerPhone,
    c.Email AS CustomerEmail,
    o.InstallationAddress,
    p.Name AS PlanName,
    p.ConnectionType,
    p.MonthlyRental,
    p.SecurityDeposit,
    cc.Status AS ConnectionStatus,
    cc.IpAddress,
    cc.PortNumber,
    eq.SerialNumber AS DeviceSerial,
    eq.DeviceModel,
    rs.Name AS RetailStoreName,
    cc.InstalledDate,
    cc.LastUpdated
FROM dbo.CustomerConnection cc
INNER JOIN dbo.Orders o ON cc.OrderID = o.OrderID
INNER JOIN dbo.Customer c ON o.CustomerID = c.CustomerID
INNER JOIN dbo.ServicePlan p ON o.PlanID = p.PlanID
INNER JOIN dbo.RetailStore rs ON o.StoreID = rs.StoreID
LEFT JOIN dbo.Equipment eq ON cc.EquipmentID = eq.EquipmentID;
GO

-- 4.2. Invoices & Aging Report View
CREATE OR ALTER VIEW dbo.vw_BillingSummary
AS
SELECT 
    b.BillID,
    b.InvoiceNumber,
    b.AccountID,
    c.FullName AS CustomerName,
    c.CustomerType,
    b.BillingMonth,
    b.BillingDate,
    b.DueDate,
    b.Subtotal,
    b.ServiceTaxAmount,
    b.LateFeeAmount,
    b.TotalAmount,
    b.AmountPaid,
    b.DueAmount,
    b.Status AS BillStatus,
    CASE 
        WHEN b.Status = 'Paid' THEN 0
        WHEN DATEDIFF(DAY, b.DueDate, GETDATE()) > 0 THEN DATEDIFF(DAY, b.DueDate, GETDATE())
        ELSE 0
    END AS DaysOverdue
FROM dbo.Bill b
INNER JOIN dbo.CustomerConnection cc ON b.AccountID = cc.AccountID
INNER JOIN dbo.Orders o ON cc.OrderID = o.OrderID
INNER JOIN dbo.Customer c ON o.CustomerID = c.CustomerID;
GO

-- 4.3. Retail Store Performance Statistics View
CREATE OR ALTER VIEW dbo.vw_RetailStoreStats
AS
SELECT 
    rs.StoreID,
    rs.Name AS StoreName,
    rs.City,
    rs.CityCode,
    rs.ManagerName,
    rs.Status AS StoreStatus,
    COUNT(DISTINCT e.EmployeeID) AS ActualEmployeeCount,
    COUNT(DISTINCT o.OrderID) AS TotalOrdersCount,
    COUNT(DISTINCT cc.AccountID) AS TotalSubscribersCount
FROM dbo.RetailStore rs
LEFT JOIN dbo.Employee e ON rs.StoreID = e.StoreID AND e.Status = 'Active'
LEFT JOIN dbo.Orders o ON rs.StoreID = o.StoreID
LEFT JOIN dbo.CustomerConnection cc ON o.OrderID = cc.OrderID
GROUP BY rs.StoreID, rs.Name, rs.City, rs.CityCode, rs.ManagerName, rs.Status;
GO

-- 4.4. Inventory Stock & Hardware View
CREATE OR ALTER VIEW dbo.vw_InventoryStock
AS
SELECT 
    inv.InventoryID,
    inv.ItemCode,
    inv.Name AS ItemName,
    inv.Category,
    inv.StockQuantity,
    inv.ReorderLevel,
    inv.UnitCost,
    (inv.StockQuantity * inv.UnitCost) AS TotalStockValue,
    v.CompanyName AS SupplierName,
    inv.Location,
    CASE WHEN inv.StockQuantity <= inv.ReorderLevel THEN 1 ELSE 0 END AS IsLowStock
FROM dbo.InventoryItem inv
LEFT JOIN dbo.Vendor v ON inv.VendorID = v.VendorID;
GO

-- ===================================================================================
-- 5. COMPREHENSIVE SEED DATA (CLEAN UTF-8, NO SPECIAL CHARACTER MOJIBAKE)
-- ===================================================================================

PRINT '>> Inserting Clean Comprehensive Seed Data...';

-- 5.1. RETAIL OUTLETS (4 Cửa hàng bán lẻ)
INSERT INTO dbo.RetailStore (StoreID, Name, City, CityCode, Address, ManagerName, Phone, Email, OperatingHours, Status, ActiveEmployeesCount, TotalSubscribersServed)
VALUES
('SH-01', N'Downtown Nexus Flagship Store', N'New York', '064', N'452 Broadway, Manhattan, NY 10013', N'David Chen', '+1 (212) 555-0144', 'flagship.sh01@nexus.telecom', N'Mon-Sat: 08:30 - 20:00, Sun: 10:00 - 18:00', 'Active', 6, 1420),
('SH-02', N'Metro Uptown Tech Hub', N'New York', '064', N'2190 Broadway, Upper West Side, NY 10024', N'Aiden Brooks', '+1 (212) 555-0189', 'uptown.sh02@nexus.telecom', N'Mon-Sat: 09:00 - 19:30', 'Active', 4, 980),
('SH-03', N'Queens Central Service Center', N'Queens', '072', N'70-20 Austin St, Forest Hills, NY 11375', N'Kavita Patel', '+1 (718) 555-0199', 'queens.sh03@nexus.telecom', N'Mon-Fri: 09:00 - 18:00, Sat: 09:00 - 15:00', 'Active', 3, 750),
('SH-04', N'Brooklyn Nexus Connect Depot', N'Brooklyn', '081', N'320 Atlantic Ave, Boerum Hill, NY 11201', N'Robert Gomez', '+1 (718) 555-0210', 'brooklyn.sh04@nexus.telecom', N'Mon-Sat: 09:00 - 19:00', 'Active', 5, 1120);

-- 5.2. EMPLOYEES (5 Nhân viên nòng cốt các bộ phận)
INSERT INTO dbo.Employee (EmployeeID, EmployeeCode, FullName, Email, Phone, PasswordHash, Role, Department, StoreID, Status, DateOfJoining, AvatarUrl, Address, Gender, DateOfBirth, Bio)
VALUES
('emp-01', 'EMP-1001', N'Sarah Jenkins', 'sarah.jenkins@nexus.telecom', '+1 (555) 234-8901', '$2a$11$MQZaoLKrxeF1DJZMGT/PkONm.ppYEF8VCS4mYIbi8C/PNPsaPSaxK', 'Manager', N'Administration', NULL, 'Active', '2022-03-15', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=150', N'742 Evergreen Terrace, NY', N'Female', '1988-04-12', N'Senior Operations Manager leading overall telecommunication infrastructure and store network.'),
('emp-02', 'EMP-1042', N'David Chen', 'david.chen@nexus.telecom', '+1 (555) 456-1123', '$2a$11$MQZaoLKrxeF1DJZMGT/PkONm.ppYEF8VCS4mYIbi8C/PNPsaPSaxK', 'Retail Staff', N'Retail Outlets', 'SH-01', 'Active', '2023-06-10', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=150', N'120 Broadway, Manhattan, NY', N'Male', '1992-09-25', N'Flagship Store lead customer representative specialized in corporate bulk schemes.'),
('emp-03', 'EMP-1077', N'Marcus Ramirez', 'marcus.ramirez@nexus.telecom', '+1 (555) 789-3344', '$2a$11$MQZaoLKrxeF1DJZMGT/PkONm.ppYEF8VCS4mYIbi8C/PNPsaPSaxK', 'Field Engineer', N'Technical Operations', 'SH-01', 'Active', '2021-11-04', 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=150', N'88 Bedford Ave, Brooklyn, NY', N'Male', '1990-12-05', N'Lead technical supervisor handling fiber splicing, line attenuation testing, and DP box routing.'),
('emp-04', 'EMP-1090', N'Elena Rostova', 'elena.rostova@nexus.telecom', '+1 (555) 901-5567', '$2a$11$MQZaoLKrxeF1DJZMGT/PkONm.ppYEF8VCS4mYIbi8C/PNPsaPSaxK', 'Senior Accountant', N'Finance & Accounts', NULL, 'Active', '2020-08-20', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=150', N'45 Wall Street, Suite 900, NY', N'Female', '1985-02-18', N'Head of Billing and Financial Accounts handling ledger audits, invoices, and service tax returns.'),
('emp-05', 'EMP-1105', N'Aiden Brooks', 'aiden.brooks@nexus.telecom', '+1 (555) 345-6789', '$2a$11$MQZaoLKrxeF1DJZMGT/PkONm.ppYEF8VCS4mYIbi8C/PNPsaPSaxK', 'Retail Staff', N'Retail Outlets', 'SH-02', 'Active', '2024-01-15', 'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=150', N'220 W 98th St, NY', N'Male', '1995-07-14', N'Customer support specialist and subscriber onboarding executive.');

-- 5.3. VENDORS (4 Nhà cung cấp thiết bị)
INSERT INTO dbo.Vendor (VendorID, VendorCode, CompanyName, ContactPerson, Category, Phone, Email, Address, TaxNumber, Rating, Status)
VALUES
('vnd-01', 'VND-401', N'Corning Optical Systems Ltd', N'Gregory Vance', N'Fiber Optics & Cabling', '+1 (800) 522-6789', 'sales@corning-telecom.com', N'800 Corning Way, Hickory, NC', 'US-TAX-99281', 5, 'Active'),
('vnd-02', 'VND-402', N'Cisco Systems Commercial Hardware', N'Linda Morrison', N'Modems & Routers', '+1 (800) 553-6387', 'enterprise-hw@cisco.com', N'170 West Tasman Dr, San Jose, CA', 'US-TAX-88192', 5, 'Active'),
('vnd-03', 'VND-403', N'Zyxel Communications Corp', N'Kenji Sato', N'Modems & Routers', '+1 (714) 632-0882', 'support-b2b@zyxel.com', N'1130 North Miller St, Anaheim, CA', 'US-TAX-77123', 4, 'Active'),
('vnd-04', 'VND-404', N'Amphenol Telecom Assemblies', N'Rachel Ward', N'Telecom Switches', '+1 (203) 265-8900', 'supply@amphenol-rf.com', N'358 Hall Avenue, Wallingford, CT', 'US-TAX-66512', 4, 'Active');

-- 5.4. SERVICE PLANS (16 Gói cước viễn thông tiêu chuẩn)
INSERT INTO dbo.ServicePlan (PlanID, PlanCode, Name, ConnectionType, SpeedOrBandwidth, MonthlyRental, HourlyCharge, SecurityDeposit, DataLimit, BillingCycle, Validity, IncludedHours, CallRates, Description, IsPopular, Status)
VALUES
('plan-du-h10', 'DU-H10', N'Dial-Up Hourly 10 Hrs', 'Dial-Up', '56 Kbps V.92', 50.00, 5.00, 325.00, '10 Hours', 'Hourly Pack', '1 Month', 10, NULL, N'Prepaid 10-hour dial-up pack, valid for one month.', 0, 'Active'),
('plan-du-h30', 'DU-H30', N'Dial-Up Hourly 30 Hrs', 'Dial-Up', '56 Kbps V.92', 130.00, 4.33, 325.00, '30 Hours', 'Hourly Pack', '3 Months', 30, NULL, N'Prepaid 30-hour dial-up pack, valid for three months.', 0, 'Active'),
('plan-du-h60', 'DU-H60', N'Dial-Up Hourly 60 Hrs', 'Dial-Up', '56 Kbps V.92', 260.00, 4.33, 325.00, '60 Hours', 'Hourly Pack', '6 Months', 60, NULL, N'Prepaid 60-hour dial-up pack, valid for six months.', 0, 'Active'),
('plan-du-h100', 'DU-H100', N'Dial-Up Hourly 100 Hrs', 'Dial-Up', '56 Kbps V.92', 400.00, 4.00, 325.00, '100 Hours', 'Hourly Pack', '1 Year', 100, NULL, N'Prepaid 100-hour dial-up pack, valid for one year.', 0, 'Active'),
('plan-du-28', 'DU-U28', N'Dial-Up Unlimited 28 Kbps', 'Dial-Up', '28 Kbps', 75.00, 0.00, 325.00, 'Unlimited', 'Monthly', '1 Month', 0, NULL, N'Unlimited dial-up access at 28 Kbps. Quarterly billing also available at $150.', 0, 'Active'),
('plan-du-56', 'DU-U56', N'Dial-Up Unlimited 56 Kbps', 'Dial-Up', '56 Kbps', 100.00, 0.00, 325.00, 'Unlimited', 'Monthly', '1 Month', 0, NULL, N'Unlimited dial-up access at 56 Kbps. Quarterly billing also available at $180.', 1, 'Active'),
('plan-bb-h30', 'BB-H30', N'Broadband Hourly 30 Hrs', 'Broadband', 'Broadband', 175.00, 5.83, 500.00, '30 Hours', 'Hourly Pack', '1 Month', 30, NULL, N'Prepaid 30-hour broadband pack, valid for one month.', 0, 'Active'),
('plan-bb-h60', 'BB-H60', N'Broadband Hourly 60 Hrs', 'Broadband', 'Broadband', 315.00, 5.25, 500.00, '60 Hours', 'Hourly Pack', '6 Months', 60, NULL, N'Prepaid 60-hour broadband pack, valid for six months.', 0, 'Active'),
('plan-bb-h120', 'BB-H120', N'Broadband Hourly 120 Hrs', 'Broadband', 'Broadband', 500.00, 4.17, 500.00, '120 Hours', 'Hourly Pack', '3 Months', 120, NULL, N'Prepaid 120-hour broadband pack, valid for three months.', 0, 'Active'),
('plan-bb-64', 'BB-U64', N'Broadband Unlimited 64 Kbps', 'Broadband', '64 Kbps', 225.00, 0.00, 500.00, 'Unlimited', 'Monthly', '1 Month', 0, NULL, N'Unlimited broadband access at 64 Kbps. Quarterly billing available at $400.', 1, 'Active'),
('plan-bb-128', 'BB-U128', N'Broadband Unlimited 128 Kbps', 'Broadband', '128 Kbps', 350.00, 0.00, 500.00, 'Unlimited', 'Monthly', '1 Month', 0, NULL, N'Unlimited broadband access at 128 Kbps. Quarterly billing available at $445.', 1, 'Active'),
('plan-ll-loc-a', 'LL-LOC-A', N'Landline Local Plan A', 'Landline', 'PSTN Voice', 25.00, 0.00, 250.00, 'Local Calling', 'Monthly', '1 Month', 0, N'Local: $1.00/min', N'Local plan, monthly rental. Call charges billed on top of rental.', 0, 'Active'),
('plan-ll-loc-u', 'LL-LOC-U', N'Landline Local Unlimited', 'Landline', 'PSTN Voice', 75.00, 0.00, 250.00, 'Local Calling', 'Yearly', '1 Year', 0, N'Local: 55c/min', N'Local plan, yearly rental per spec. Call charges billed on top of rental.', 0, 'Active'),
('plan-ll-loc-b', 'LL-LOC-B', N'Landline Local Plan B', 'Landline', 'PSTN Voice', 35.00, 0.00, 250.00, 'Local Calling', 'Monthly', '1 Month', 0, N'Local: 75c/min', N'Local plan with discounted call charges.', 0, 'Active'),
('plan-ll-std-m', 'LL-STD-M', N'Landline STD - Monthly', 'Landline', 'PSTN Voice', 125.00, 0.00, 250.00, 'Local + STD', 'Monthly', '1 Month', 0, N'Local: 70c/min | STD: $2.25/min | SMS to mobile: $1.00/min', N'STD plan, monthly rental with local, STD and mobile-messaging call charges.', 1, 'Active'),
('plan-ll-std-h', 'LL-STD-H', N'Landline STD - Half-Yearly', 'Landline', 'PSTN Voice', 420.00, 0.00, 250.00, 'Local + STD', 'Half-Yearly', '6 Months', 0, N'Local: 60c/min | STD: $2.00/min | SMS to mobile: $1.15/min', N'STD plan, half-yearly rental with reduced call charges.', 0, 'Active'),
('plan-ll-std-y', 'LL-STD-Y', N'Landline STD - Yearly', 'Landline', 'PSTN Voice', 780.00, 0.00, 250.00, 'Local + STD', 'Yearly', '1 Year', 0, N'Local: 60c/min | STD: $1.75/min | SMS to mobile: $1.25/min', N'STD plan, yearly rental with the lowest call charges.', 0, 'Active');

-- 5.5. INVENTORY ITEMS (5 Loại vật tư kho hàng - Tab Stock của Admin)
INSERT INTO dbo.InventoryItem (InventoryID, ItemCode, Name, Category, StockQuantity, ReorderLevel, UnitCost, Location, VendorID, Status)
VALUES
('inv-01', 'EQ-ONT-FBR',  N'Huawei EchoLife HG8245H5 GPON ONT',            'Fiber ONT',    145, 25, 55.00, N'Central Depot Bay 4A', 'vnd-02', 'In Stock'),
('inv-02', 'EQ-RTR-AX',   N'Nexus Wi-Fi 6 AX3000 Dual-Band Router',        'Router',        88, 20, 65.00, N'Central Depot Bay 2B', 'vnd-03', 'In Stock'),
('inv-03', 'EQ-MDM-V92',  N'USRobotics 56K V.92 Faxmodem USB/PSTN',        'Modem',         18, 10, 32.00, N'Central Depot Bay 7C', 'vnd-04', 'In Stock'),
('inv-04', 'EQ-ATA-VOIP', N'Grandstream HT802 2-Port Analog VoIP Adapter', 'VoIP Adapter',  42, 15, 28.00, N'Central Depot Bay 3C', 'vnd-04', 'In Stock'),
('inv-05', 'EQ-SPL-1X8',  N'Corning 1x8 PLC Optical Fiber Splitter',       'Splitter',       9, 15, 14.50, N'Central Depot Bay 1A', 'vnd-01', 'In Stock');

-- 5.6. CUSTOMERS (7 Khách hàng cá nhân & doanh nghiệp)
INSERT INTO dbo.Customer (CustomerID, FullName, CustomerType, Email, ContactNumber, Address, BillingAddress, TaxNumber, IdProofType, IdProofNumber, Status)
VALUES
('CUST-0001', N'Arthur Pendelton', 'Individual', 'arthur.p@classiccorp.net', '+1 (555) 902-1844', N'144 West 82nd St, Apt 4B, New York, NY 10024', NULL, NULL, 'National ID Card', 'ID-US-9918231', 'Active'),
('CUST-0002', N'Samantha Vance', 'Individual', 'samantha.vance@gmail.com', '+1 (555) 301-4477', N'78 Mercer St, Soho, New York, NY 10012', NULL, NULL, 'Passport', 'P-98827419', 'Active'),
('CUST-0003', N'Highline Consulting LLC', 'Corporate', 'office@highlineconsulting.com', '+1 (555) 777-8899', N'55 Hudson Yards, Fl 18, New York, NY 10001', N'55 Hudson Yards, Finance Dept, NY 10001', 'TAX-CORP-44910', 'Driver''s License', 'DL-NY-2940192', 'Active'),
('CUST-0004', N'Robert Lewandowski', 'Individual', 'robert.lewan@yahoo.com', '+1 (555) 621-9988', N'89-12 Far Rockaway Blvd, Queens, NY 11693', NULL, NULL, 'National ID Card', 'ID-US-8827391', 'Active'),
('CUST-0005', N'Victoria Sterling', 'Individual', 'v.sterling@apexlegal.org', '+1 (555) 441-2099', N'120 E 64th St, Manhattan, NY 10065', NULL, NULL, 'National ID Card', 'ID-US-7721890', 'Active'),
('CUST-0006', N'Retro Arcade Lounge LLC', 'Corporate', 'manager@retroarcadeny.com', '+1 (555) 332-9011', N'31 St Marks pl, East Village, NY 10003', N'31 St Marks pl, NY 10003', 'TAX-CORP-33109', 'Driver''s License', 'DL-NY-3391024', 'Active'),
('CUST-0007', N'Jonathan Meyer', 'Individual', 'j.meyer@brooklynloft.io', '+1 (555) 881-2300', N'175 Water St, Dumbo, Brooklyn, NY 11201', NULL, NULL, 'Passport', 'P-77123984', 'Active');

-- 5.7. ORDERS (7 Đơn hàng kèm kết quả khảo sát kỹ thuật)
INSERT INTO dbo.Orders (OrderID, CustomerID, PlanID, StoreID, EmployeeID, InstallationAddress, ApplicationDate, Status, CableDistanceMeters, DpBoxCapacity, SignalLossDbm, FeasibilityNotes, FeasibilityCheckedBy, FeasibilityCheckedDate, ScheduledInstallDate, RejectionReason, BulkConnectionsCount, BulkDiscountPercent, ExistingLandlineAccountId, LandlineFeasible, InternetFeasible, AssignedAccountId)
VALUES
('D0000000001', 'CUST-0001', 'plan-du-56', 'SH-02', 'emp-02', N'144 West 82nd St, Apt 4B, New York, NY 10024', '2026-09-04 10:30:00', 'Pending', 420, N'Port 6 Available / DP-B12', -18.50, NULL, NULL, NULL, NULL, NULL, 1, 0.00, NULL, NULL, NULL, NULL),
('B0000000002', 'CUST-0002', 'plan-bb-128', 'SH-01', 'emp-02', N'78 Mercer St, Soho, New York, NY 10012', '2026-09-04 14:15:00', 'Feasible', 85, N'Port 2 Available / DP-S04', -16.20, N'Fiber termination box available within 85m. Signal strength -16.2 dBm (Excellent). Line tested OK.', 'emp-03', '2026-09-04 16:00:00', '2026-09-08', NULL, 1, 0.00, NULL, NULL, 1, 'B064-000000000005'),
('T0000000003', 'CUST-0003', 'plan-ll-std-m', 'SH-01', 'emp-02', N'55 Hudson Yards, Fl 18, New York, NY 10001', '2026-09-02 09:00:00', 'Connection Provided', 120, N'Port 8 Dedicated', -15.10, N'Copper loop line deployed. Line tested and audio quality verified.', 'emp-03', '2026-09-02 11:30:00', '2026-09-02', NULL, 50, 75.00, NULL, 1, NULL, 'T064-000000000001'),
('B0000000004', 'CUST-0004', 'plan-bb-64', 'SH-03', 'emp-05', N'89-12 Far Rockaway Blvd, Queens, NY 11693', '2026-09-03 11:45:00', 'Not Feasible', 1150, N'No Spare Ports', -34.00, N'Distance to nearest fiber distribution box exceeds 1,150 meters. Severe optical attenuation (-34 dBm). Requires main trunk extension.', 'emp-03', '2026-09-03 15:00:00', NULL, N'Distance > 1km and optical loss exceeds threshold (-34 dBm)', 50, 75.00, NULL, NULL, 0, NULL),
('B0000000005', 'CUST-0005', 'plan-bb-64', 'SH-01', 'emp-02', N'120 E 64th St, Manhattan, NY 10065', '2026-08-14 10:00:00', 'Connection Provided', 60, N'Port 1 Dedicated / DP-M01', -14.80, N'Direct splice into riser. Verified gigabit throughput.', 'emp-03', '2026-08-14 14:00:00', '2026-08-15', NULL, 50, 75.00, NULL, NULL, 1, 'B064-000000000002'),
('D0000000006', 'CUST-0006', 'plan-du-56', 'SH-01', 'emp-02', N'31 St Marks pl, East Village, NY 10003', '2026-07-08 15:30:00', 'Connection Provided', 150, N'PSTN Riser Port 3', -17.20, N'PSTN copper pair active.', 'emp-03', '2026-07-09 10:00:00', '2026-07-10', NULL, 50, 75.00, NULL, 1, 1, 'D064-000000000003'),
('B0000000007', 'CUST-0007', 'plan-bb-64', 'SH-04', 'emp-05', N'175 Water St, Dumbo, Brooklyn, NY 11201', '2026-05-15 11:00:00', 'Connection Provided', 90, N'DP-BK-11 Port 4', -15.50, N'Initial install verified.', 'emp-03', '2026-05-16 11:00:00', '2026-05-18', NULL, 50, 75.00, NULL, NULL, 1, 'B081-000000000004');

-- 5.8. EQUIPMENT (6 Thiết bị CPE gán thuê bao hoặc trong kho - mỗi router gán tối đa 1 connection)
INSERT INTO dbo.Equipment (EquipmentID, InventoryID, SerialNumber, MacAddress, DeviceModel, DeviceType, VendorID, StoreID, Status, FirmwareVersion, AssignedAccountId, AssignedTechnicianId, InstalledDate)
VALUES
('eq-01', 'inv-01', 'NX-HW-992810', 'BC:A9:93:21:44:8E', N'Huawei EchoLife HG8245H5 GPON ONT',            'Fiber ONT Modem',          'vnd-02', 'SH-01', 'In Service', 'V500R019C20SPC120', 'B064-000000000002', 'emp-03', '2026-08-15 11:20:00'),
('eq-02', 'inv-04', 'NX-ATA-881920', '00:0B:82:76:D4:11', N'Grandstream HT802 2-Port Analog VoIP Adapter',  'Analog Telephone Adapter', 'vnd-04', 'SH-01', 'In Service', '1.0.35.3',          'T064-000000000001', 'emp-03', '2026-09-02 16:30:00'),
('eq-03', 'inv-03', 'NX-MD-110294',  'F8:E4:FB:99:A2:03', N'USRobotics 56K V.92 Faxmodem USB/PSTN',         'VDSL2/ADSL Modem',         'vnd-04', 'SH-01', 'In Service', 'v2.1.8-PSTN',       'D064-000000000003', 'emp-03', '2026-07-10 14:00:00'),
('eq-04', 'inv-02', 'NX-HW-992811',  '00:1A:2B:3C:4D:5E', N'Nexus Wi-Fi 6 AX3000 Dual-Band Router',         'Gigabit Router',           'vnd-03', 'SH-01', 'In Stock',   'v3.2.4-BUILD-921',  NULL,                NULL,     NULL),
('eq-05', 'inv-01', 'NX-HW-992812',  '54:AF:97:88:B1:00', N'Huawei EchoLife HG8245H5 GPON ONT',             'Fiber ONT Modem',          'vnd-02', 'SH-01', 'In Stock',   'V500R019C20SPC120', NULL,                NULL,     NULL),
('eq-06', 'inv-02', 'NX-HW-992813',  'A0:B1:C2:D3:E4:F5', N'Nexus Wi-Fi 6 AX3000 Dual-Band Router',         'Gigabit Router',           'vnd-03', 'SH-01', 'In Service', 'v3.2.4-BUILD-921',  'T064-000000000008', 'emp-03', '2026-09-02 16:30:00');

-- 5.9. CUSTOMER CONNECTIONS (Thuê bao đường truyền: minh họa 1 Order T0000000003 gắn nhiều Connection, mỗi Connection 1 Router riêng)
INSERT INTO dbo.CustomerConnection (AccountID, OrderID, EquipmentID, Status, IpAddress, PortNumber, InstalledDate, SuspensionStartDate, SuspensionEndDate, TerminatedDate, LastUpdated, LastStatusReason)
VALUES
('T064-000000000001', 'T0000000003', 'eq-02', 'Active', '198.51.100.42', 'VOIP-ETH-1', '2026-09-02 09:00:00', NULL, NULL, NULL, '2026-09-02 16:30:00', NULL),
('T064-000000000008', 'T0000000003', 'eq-06', 'Active', '198.51.100.43', 'VOIP-ETH-2', '2026-09-02 09:30:00', NULL, NULL, NULL, '2026-09-02 16:30:00', NULL),
('B064-000000000002', 'B0000000005', 'eq-01', 'Active', '203.0.113.88', 'GPON-0/1/4', '2026-08-15 10:00:00', NULL, NULL, NULL, '2026-08-15 11:20:00', NULL),
('D064-000000000003', 'D0000000006', 'eq-03', 'Temporarily Inactive', '192.0.2.14', 'PSTN-LINE-4', '2026-07-10 14:00:00', '2026-09-01', '2026-10-31', NULL, '2026-09-01 09:15:00', N'Customer requested seasonal suspension during venue renovation.'),
('B081-000000000004', 'B0000000007', NULL, 'Permanently Inactive', NULL, NULL, '2026-05-18 11:00:00', NULL, NULL, '2026-08-30 17:00:00', '2026-08-30 17:00:00', N'Tenant relocated outside coverage zone; equipment returned and de-provisioned.');

-- 5.10. BILLS & INVOICES (2 Hóa đơn cước)
INSERT INTO dbo.Bill (BillID, InvoiceNumber, AccountID, BillingMonth, BillingPeriodStart, BillingPeriodEnd, BillingDate, DueDate, SecurityDeposit, MonthlyRental, HourlyCharges, DiscountPercent, DiscountAmount, Subtotal, ServiceTaxRate, ServiceTaxAmount, LateFeeAmount, TotalAmount, AmountPaid, DueAmount, Status, GeneratedDate)
VALUES
('bill-01', 'NEX-INV-2026-001', 'B064-000000000002', 'August 2026', '2026-08-01', '2026-08-31', '2026-08-15', '2026-09-05', 500.00, 225.00, 0.00, 0.00, 0.00, 725.00, 12.24, 88.74, 0.00, 813.74, 813.74, 0.00, 'Paid', '2026-08-15 10:30:00'),
('bill-02', 'NEX-INV-2026-002', 'T064-000000000001', 'September 2026', '2026-09-01', '2026-09-30', '2026-09-02', '2026-09-22', 250.00, 125.00, 0.00, 25.00, 93.75, 281.25, 12.24, 34.43, 0.00, 315.68, 100.00, 215.68, 'Partially Paid', '2026-09-02 09:30:00');

-- 5.11. PAYMENTS (2 Biên lai thanh toán)
INSERT INTO dbo.Payment (PaymentID, ReceiptNumber, BillID, EmployeeID, AmountPaid, PaymentDate, PaymentMode, ReferenceNumber, Status, Notes)
VALUES
('PAY-89201', 'REC-2026-00891', 'bill-01', 'emp-04', 813.74, '2026-08-20 14:10:00', 'Credit/Debit Card', 'TXN-VISA-994821', 'Completed', N'Paid in full via Visa debit at counter.'),
('PAY-89205', 'REC-2026-00895', 'bill-02', 'emp-04', 100.00, '2026-09-03 11:20:00', 'Bank Transfer/NEFT', 'ACH-CITI-449102', 'Completed', N'Partial payment on corporate account.');

-- 5.12. CUSTOMER FEEDBACK (2 Phản hồi chất lượng dịch vụ)
INSERT INTO dbo.Feedback (FeedbackID, AccountID, OrderID, CustomerID, CustomerName, Rating, Category, Message, Status, CreatedAt, Response, RespondedBy, RespondedAt)
VALUES
('fb-01', 'B064-000000000002', 'B0000000005', 'CUST-0005', N'Victoria Sterling', 5, 'Installation', N'Field engineer arrived on time and the fibre line was live within an hour. Very smooth.', 'Resolved', '2026-08-16 09:12:00', N'Thank you for the kind words - we have shared this with the SH-01 install team.', 'emp-01', '2026-08-16 15:40:00'),
('fb-02', 'D064-000000000003', 'D0000000006', 'CUST-0006', N'Retro Arcade Lounge LLC', 3, 'Support', N'Took two calls to get the seasonal suspension applied. Please make this self-service.', 'Under Review', '2026-09-01 11:05:00', NULL, NULL, NULL);

-- 5.13. SYSTEM SETTINGS (6 Tham số hệ thống)
INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Category, DataType, Description, LastUpdated)
VALUES
('ServiceTaxRate', '12.24', 'Billing', 'number', N'Statutory telecommunication service tax rate (12.24%)', GETDATE()),
('LatePaymentFeePercent', '5.00', 'Billing', 'number', N'Late payment surcharge rate applied after invoice due date', GETDATE()),
('DepositBroadband', '500.00', 'Technical', 'number', N'Standard security deposit for Broadband connections ($)', GETDATE()),
('DepositDialUp', '325.00', 'Technical', 'number', N'Standard security deposit for Dial-Up connections ($)', GETDATE()),
('DepositLandline', '250.00', 'Technical', 'number', N'Standard security deposit for Landline telephone connections ($)', GETDATE()),
('InstallationGracePeriodDays', '7', 'Technical', 'number', N'Number of days allotted for field installation after feasibility approval', GETDATE());

-- 5.14. CONNECTION ACTIVITY LOGS (4 Nhật ký biến động thuê bao)
INSERT INTO dbo.ConnectionActivityLog (AccountID, ActionType, OldValue, NewValue, Reason, PerformedBy, Timestamp)
VALUES
('T064-000000000001', 'Provisioning', NULL, 'Active', N'Initial copper loop connection provisioned.', 'emp-03', '2026-09-02 09:00:00'),
('B064-000000000002', 'Provisioning', NULL, 'Active', N'Gigabit fiber drop installed with Huawei ONT.', 'emp-03', '2026-08-15 10:00:00'),
('D064-000000000003', 'StatusChange', 'Active', 'Temporarily Inactive', N'Customer requested temporary suspension for venue renovation.', 'emp-02', '2026-09-01 09:15:00'),
('B081-000000000004', 'StatusChange', 'Active', 'Permanently Inactive', N'Customer relocated outside franchise zone; de-provisioned and equipment retrieved.', 'emp-03', '2026-08-30 17:00:00');
GO

PRINT '>> [NexusSystem] Database upgraded successfully with 3NF Zero-Redundancy schema, Audit Logs, and Views!';
GO

-- 5.13. PURCHASE ORDERS (2 đơn đặt mua thiết bị mẫu)
INSERT INTO dbo.PurchaseOrder (PurchaseOrderID, VendorID, StoreID, ItemCode, ItemName, Category, Quantity, UnitCost, TotalCost, OrderDate, ExpectedDate, ReceivedDate, Status, Notes, CreatedBy)
VALUES
('PO-20260910-0001', (SELECT TOP 1 VendorID FROM dbo.Vendor WHERE Category LIKE '%Modem%'), 'SH-01', 'EQ-MDM-100', N'AX-100 Fiber Modem', N'Modem', 50, 18.50, 925.00, '2026-09-10', '2026-09-20', '2026-09-18', 'Received', N'Nhập lô modem cho gói cước mới tháng 9', 'emp-01'),
('PO-20260922-0002', (SELECT TOP 1 VendorID FROM dbo.Vendor WHERE Category LIKE '%Router%'), 'SH-02', 'EQ-RTR-200', N'RT-2600 Gigabit Router', N'Router', 25, 42.00, 1050.00, '2026-09-22', '2026-10-05', NULL, 'Submitted', N'Đặt thêm router cho các đơn broadband cuối tháng', 'emp-01');
