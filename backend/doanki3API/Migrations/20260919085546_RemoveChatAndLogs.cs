using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace doanki3API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveChatAndLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customer",
                columns: table => new
                {
                    CustomerID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CustomerType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Individual"),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    ContactNumber = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BillingAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TaxNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IdProofType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "National ID Card"),
                    IdProofNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customer", x => x.CustomerID);
                });

            migrationBuilder.CreateTable(
                name: "RetailStore",
                columns: table => new
                {
                    StoreID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CityCode = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ManagerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    OperatingHours = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    ActiveEmployeesCount = table.Column<int>(type: "int", nullable: false),
                    TotalSubscribersServed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailStore", x => x.StoreID);
                });

            migrationBuilder.CreateTable(
                name: "ServicePlan",
                columns: table => new
                {
                    PlanID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PlanCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ConnectionType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    SpeedOrBandwidth = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    MonthlyRental = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HourlyCharge = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataLimit = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    BillingCycle = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Monthly"),
                    Validity = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IncludedHours = table.Column<int>(type: "int", nullable: false),
                    CallRates = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPopular = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePlan", x => x.PlanID);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    SettingKey = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "General"),
                    DataType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "string"),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.SettingKey);
                });

            migrationBuilder.CreateTable(
                name: "Vendor",
                columns: table => new
                {
                    VendorID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    VendorCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    TaxNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)5),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendor", x => x.VendorID);
                });

            migrationBuilder.CreateTable(
                name: "Employee",
                columns: table => new
                {
                    EmployeeID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EmployeeCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StoreID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Active"),
                    DateOfJoining = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(CONVERT([date],getdate()))"),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employee", x => x.EmployeeID);
                    table.ForeignKey(
                        name: "FK_Employee_RetailStore",
                        column: x => x.StoreID,
                        principalTable: "RetailStore",
                        principalColumn: "StoreID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItem",
                columns: table => new
                {
                    InventoryID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ItemCode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 10),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VendorID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "In Stock"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItem", x => x.InventoryID);
                    table.ForeignKey(
                        name: "FK_InventoryItem_Vendor",
                        column: x => x.VendorID,
                        principalTable: "Vendor",
                        principalColumn: "VendorID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    OrderID = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    CustomerID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    PlanID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    StoreID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    EmployeeID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    InstallationAddress = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ApplicationDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Pending"),
                    CableDistanceMeters = table.Column<int>(type: "int", nullable: true),
                    DpBoxCapacity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SignalLossDbm = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    FeasibilityNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FeasibilityCheckedBy = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    FeasibilityCheckedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ScheduledInstallDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BulkConnectionsCount = table.Column<int>(type: "int", nullable: false, defaultValue: 50),
                    BulkDiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 75.00m),
                    ExistingLandlineAccountId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    LandlineFeasible = table.Column<bool>(type: "bit", nullable: true),
                    InternetFeasible = table.Column<bool>(type: "bit", nullable: true),
                    AssignedAccountId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_Orders_Customer",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID");
                    table.ForeignKey(
                        name: "FK_Orders_Employee",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_FeasibilityEngineer",
                        column: x => x.FeasibilityCheckedBy,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID");
                    table.ForeignKey(
                        name: "FK_Orders_RetailStore",
                        column: x => x.StoreID,
                        principalTable: "RetailStore",
                        principalColumn: "StoreID");
                    table.ForeignKey(
                        name: "FK_Orders_ServicePlan",
                        column: x => x.PlanID,
                        principalTable: "ServicePlan",
                        principalColumn: "PlanID");
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    EquipmentID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    InventoryID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    SerialNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    MacAddress = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    DeviceModel = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DeviceType = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    VendorID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    StoreID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "In Stock"),
                    FirmwareVersion = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    AssignedAccountId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    AssignedTechnicianId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    InstalledDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.EquipmentID);
                    table.ForeignKey(
                        name: "FK_Equipment_Employee",
                        column: x => x.AssignedTechnicianId,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Equipment_Inventory",
                        column: x => x.InventoryID,
                        principalTable: "InventoryItem",
                        principalColumn: "InventoryID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Equipment_RetailStore",
                        column: x => x.StoreID,
                        principalTable: "RetailStore",
                        principalColumn: "StoreID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Equipment_Vendor",
                        column: x => x.VendorID,
                        principalTable: "Vendor",
                        principalColumn: "VendorID");
                });

            migrationBuilder.CreateTable(
                name: "CustomerConnection",
                columns: table => new
                {
                    AccountID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    OrderID = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    EquipmentID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false, defaultValue: "Active"),
                    IpAddress = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    PortNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    InstalledDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    SuspensionStartDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    SuspensionEndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    TerminatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    LastStatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerConnection", x => x.AccountID);
                    table.ForeignKey(
                        name: "FK_CustomerConnection_Equipment",
                        column: x => x.EquipmentID,
                        principalTable: "Equipment",
                        principalColumn: "EquipmentID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CustomerConnection_Order",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID");
                });

            migrationBuilder.CreateTable(
                name: "Bill",
                columns: table => new
                {
                    BillID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    InvoiceNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AccountID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    BillingMonth = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    BillingPeriodStart = table.Column<DateOnly>(type: "date", nullable: true),
                    BillingPeriodEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    BillingDate = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "(CONVERT([date],getdate()))"),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MonthlyRental = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HourlyCharges = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ServiceTaxRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 12.24m),
                    ServiceTaxAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LateFeeAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Unpaid"),
                    GeneratedDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bill", x => x.BillID);
                    table.ForeignKey(
                        name: "FK_Bill_CustomerConnection",
                        column: x => x.AccountID,
                        principalTable: "CustomerConnection",
                        principalColumn: "AccountID");
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    FeedbackID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    AccountID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    OrderID = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: true),
                    CustomerID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CustomerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rating = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)5),
                    Category = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Resolved"),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedBy = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.FeedbackID);
                    table.ForeignKey(
                        name: "FK_Feedback_Customer",
                        column: x => x.CustomerID,
                        principalTable: "Customer",
                        principalColumn: "CustomerID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Feedback_CustomerConnection",
                        column: x => x.AccountID,
                        principalTable: "CustomerConnection",
                        principalColumn: "AccountID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Feedback_Employee",
                        column: x => x.RespondedBy,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Feedback_Orders",
                        column: x => x.OrderID,
                        principalTable: "Orders",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    PaymentID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    ReceiptNumber = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    BillID = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    EmployeeID = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    PaymentMode = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "Completed"),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payment", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_Payment_Bill",
                        column: x => x.BillID,
                        principalTable: "Bill",
                        principalColumn: "BillID");
                    table.ForeignKey(
                        name: "FK_Payment_Employee",
                        column: x => x.EmployeeID,
                        principalTable: "Employee",
                        principalColumn: "EmployeeID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bill_AccountID",
                table: "Bill",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Bill_DueDate",
                table: "Bill",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Bill_Status",
                table: "Bill",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UQ_Bill_InvoiceNumber",
                table: "Bill",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConnection_OrderID",
                table: "CustomerConnection",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerConnection_Status",
                table: "CustomerConnection",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UQ_CustomerConnection_EquipmentID",
                table: "CustomerConnection",
                column: "EquipmentID",
                unique: true,
                filter: "([EquipmentID] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Employee_StoreID",
                table: "Employee",
                column: "StoreID");

            migrationBuilder.CreateIndex(
                name: "UQ_Employee_Code",
                table: "Employee",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Employee_Email",
                table: "Employee",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_AssignedTechnicianId",
                table: "Equipment",
                column: "AssignedTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_InventoryID",
                table: "Equipment",
                column: "InventoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Status",
                table: "Equipment",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_StoreID",
                table: "Equipment",
                column: "StoreID");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_VendorID",
                table: "Equipment",
                column: "VendorID");

            migrationBuilder.CreateIndex(
                name: "UQ_Equipment_SerialNumber",
                table: "Equipment",
                column: "SerialNumber",
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_AccountID",
                table: "Feedback",
                column: "AccountID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_CustomerID",
                table: "Feedback",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_OrderID",
                table: "Feedback",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_RespondedBy",
                table: "Feedback",
                column: "RespondedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItem_Category",
                table: "InventoryItem",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItem_VendorID",
                table: "InventoryItem",
                column: "VendorID");

            migrationBuilder.CreateIndex(
                name: "UQ_InventoryItem_Code",
                table: "InventoryItem",
                column: "ItemCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerID",
                table: "Orders",
                column: "CustomerID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_EmployeeID",
                table: "Orders",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_FeasibilityCheckedBy",
                table: "Orders",
                column: "FeasibilityCheckedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PlanID",
                table: "Orders",
                column: "PlanID");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreID",
                table: "Orders",
                column: "StoreID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_BillID",
                table: "Payment",
                column: "BillID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_EmployeeID",
                table: "Payment",
                column: "EmployeeID");

            migrationBuilder.CreateIndex(
                name: "UQ_Payment_ReceiptNumber",
                table: "Payment",
                column: "ReceiptNumber",
                unique: true,
                filter: "[ReceiptNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Vendor_Code",
                table: "Vendor",
                column: "VendorCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "Bill");

            migrationBuilder.DropTable(
                name: "CustomerConnection");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "InventoryItem");

            migrationBuilder.DropTable(
                name: "Customer");

            migrationBuilder.DropTable(
                name: "Employee");

            migrationBuilder.DropTable(
                name: "ServicePlan");

            migrationBuilder.DropTable(
                name: "Vendor");

            migrationBuilder.DropTable(
                name: "RetailStore");
        }
    }
}
