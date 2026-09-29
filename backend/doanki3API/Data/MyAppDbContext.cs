using doanki3API.Models;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Data;

public partial class MyAppDbContext : DbContext
{
    public MyAppDbContext(DbContextOptions<MyAppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bill> Bills { get; set; }

    
    
    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<CustomerConnection> CustomerConnections { get; set; }

    public virtual DbSet<Employee> Employees { get; set; }

    public virtual DbSet<Equipment> Equipment { get; set; }

    public virtual DbSet<EquipmentRequest> EquipmentRequests { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<InventoryItem> InventoryItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<RetailStore> RetailStores { get; set; }

    public virtual DbSet<ServicePlan> ServicePlans { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<Vendor> Vendors { get; set; }

    public virtual DbSet<VwActiveSubscriber> VwActiveSubscribers { get; set; }

    public virtual DbSet<VwBillingSummary> VwBillingSummaries { get; set; }

    public virtual DbSet<VwInventoryStock> VwInventoryStocks { get; set; }

    public virtual DbSet<VwRetailStoreStat> VwRetailStoreStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
                modelBuilder.Entity<Bill>(entity =>
        {
            entity.ToTable("Bill");

            entity.HasIndex(e => e.AccountId, "IX_Bill_AccountID");

            entity.HasIndex(e => e.DueDate, "IX_Bill_DueDate");

            entity.HasIndex(e => e.Status, "IX_Bill_Status");

            entity.HasIndex(e => e.InvoiceNumber, "UQ_Bill_InvoiceNumber").IsUnique();

            entity.Property(e => e.BillId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("BillID");
            entity.Property(e => e.AccountId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("AccountID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BillingDate).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.BillingMonth)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.DueAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.GeneratedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.HourlyCharges).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LateFeeAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MonthlyRental).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SecurityDeposit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ServiceTaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ServiceTaxRate)
                .HasDefaultValue(12.24m)
                .HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Unpaid");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Account).WithMany(p => p.Bills)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bill_CustomerConnection");
        });

                modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customer");

            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CustomerID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.BillingAddress).HasMaxLength(255);
            entity.Property(e => e.ContactNumber)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Individual");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IdProofNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IdProofType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("National ID Card");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.TaxNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<CustomerConnection>(entity =>
        {
            entity.HasKey(e => e.AccountId);

            entity.ToTable("CustomerConnection");

            entity.HasIndex(e => e.OrderId, "IX_CustomerConnection_OrderID");

            entity.HasIndex(e => e.Status, "IX_CustomerConnection_Status");

            entity.HasIndex(e => e.EquipmentId, "UQ_CustomerConnection_EquipmentID")
                .IsUnique()
                .HasFilter("([EquipmentID] IS NOT NULL)");

            entity.Property(e => e.AccountId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("AccountID");
            entity.Property(e => e.EquipmentId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("EquipmentID");
            entity.Property(e => e.InstalledDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastStatusReason).HasMaxLength(500);
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("OrderID");
            entity.Property(e => e.PortNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.SuspensionEndDate).HasColumnType("datetime");
            entity.Property(e => e.SuspensionStartDate).HasColumnType("datetime");
            entity.Property(e => e.TerminatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.Equipment).WithOne(p => p.CustomerConnection)
                .HasForeignKey<CustomerConnection>(d => d.EquipmentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CustomerConnection_Equipment");

            entity.HasOne(d => d.Order).WithMany(p => p.CustomerConnections)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CustomerConnection_Order");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("Employee");

            entity.HasIndex(e => e.EmployeeCode, "UQ_Employee_Code").IsUnique();

            entity.HasIndex(e => e.Email, "UQ_Employee_Email").IsUnique();

            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("EmployeeID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateOfJoining).HasDefaultValueSql("(CONVERT([date],getdate()))");
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmployeeCode)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.StoreId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StoreID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Store).WithMany(p => p.Employees)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Employee_RetailStore");
        });

        modelBuilder.Entity<Equipment>(entity =>
        {
            entity.HasIndex(e => e.Status, "IX_Equipment_Status");

            entity.HasIndex(e => e.VendorId, "IX_Equipment_VendorID");

            entity.HasIndex(e => e.SerialNumber, "UQ_Equipment_SerialNumber").IsUnique();

            entity.Property(e => e.EquipmentId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("EquipmentID");
            entity.Property(e => e.AssignedAccountId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AssignedTechnicianId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DeviceModel).HasMaxLength(150);
            entity.Property(e => e.DeviceType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FirmwareVersion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InstalledDate).HasColumnType("datetime");
            entity.Property(e => e.InventoryId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("InventoryID");
            entity.Property(e => e.MacAddress)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SerialNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("In Stock");
            entity.Property(e => e.StoreId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StoreID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.VendorId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("VendorID");

            entity.HasOne(d => d.AssignedTechnician).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.AssignedTechnicianId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Equipment_Employee");

            entity.HasOne(d => d.Inventory).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Equipment_Inventory");

            entity.HasOne(d => d.Store).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Equipment_RetailStore");

            entity.HasOne(d => d.Vendor).WithMany(p => p.Equipment)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Equipment_Vendor");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("Feedback");

            entity.HasIndex(e => e.AccountId, "IX_Feedback_AccountID");

            entity.Property(e => e.FeedbackId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("FeedbackID");
            entity.Property(e => e.AccountId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("AccountID");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CustomerID");
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.OrderId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("OrderID");
            entity.Property(e => e.Rating).HasDefaultValue((byte)5);
            entity.Property(e => e.RespondedAt).HasColumnType("datetime");
            entity.Property(e => e.RespondedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Resolved");

            entity.HasOne(d => d.Account).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedback_CustomerConnection");

            entity.HasOne(d => d.Customer).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedback_Customer");

            entity.HasOne(d => d.Order).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedback_Orders");

            entity.HasOne(d => d.RespondedByNavigation).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.RespondedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Feedback_Employee");
        });

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(e => e.InventoryId);

            entity.ToTable("InventoryItem");

            entity.HasIndex(e => e.Category, "IX_InventoryItem_Category");

            entity.HasIndex(e => e.ItemCode, "UQ_InventoryItem_Code").IsUnique();

            entity.Property(e => e.InventoryId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("InventoryID");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ItemCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.ReorderLevel).HasDefaultValue(10);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("In Stock");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.VendorId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("VendorID");

            entity.HasOne(d => d.Vendor).WithMany(p => p.InventoryItems)
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_InventoryItem_Vendor");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(e => e.CustomerId, "IX_Orders_CustomerID");

            entity.HasIndex(e => e.Status, "IX_Orders_Status");

            entity.HasIndex(e => e.StoreId, "IX_Orders_StoreID");

            entity.Property(e => e.OrderId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("OrderID");
            entity.Property(e => e.ApplicationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.AssignedAccountId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BulkConnectionsCount).HasDefaultValue(50);
            // Default 0 (không giảm giá) — default 75 cũ khiến EF bỏ qua giá trị 0
            // khi INSERT (0 == CLR default) và DB tự gắn 75% cho đơn không bulk!
            entity.Property(e => e.BulkDiscountPercent)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)");
            // Cột tính toán: SQL Server tự suy từ BulkConnectionsCount (> 50 => miễn cọc).
            // Khai báo computed để EF không bao giờ INSERT/UPDATE vào cột này.
            entity.Property(e => e.DepositWaived)
                .HasComputedColumnSql("CAST(CASE WHEN [BulkConnectionsCount] > 50 THEN 1 ELSE 0 END AS BIT)");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CustomerID");
            entity.Property(e => e.DpBoxCapacity).HasMaxLength(100);
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("EmployeeID");
            entity.Property(e => e.ExistingLandlineAccountId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FeasibilityCheckedBy)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FeasibilityCheckedDate).HasColumnType("datetime");
            entity.Property(e => e.FeasibilityNotes).HasMaxLength(500);
            entity.Property(e => e.InstallationAddress).HasMaxLength(255);
            entity.Property(e => e.PlanId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PlanID");
            entity.Property(e => e.RejectionReason).HasMaxLength(500);
            entity.Property(e => e.RetailApprovedBy).HasMaxLength(100);
            entity.Property(e => e.RetailApprovedDate).HasColumnType("datetime");
            entity.Property(e => e.RetailApprovalNotes).HasMaxLength(500);
            entity.Property(e => e.AssignedTechnicianName).HasMaxLength(100);
            entity.Property(e => e.AssignedTechnicianPhone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.AssignedTechnicianDate).HasColumnType("datetime");
            entity.Property(e => e.ScheduledInstallDate).HasColumnType("datetime");
            entity.Property(e => e.SignalLossDbm).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.StoreId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StoreID");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customer");

            entity.HasOne(d => d.Employee).WithMany(p => p.OrderEmployees)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Orders_Employee");

            entity.HasOne(d => d.FeasibilityCheckedByNavigation).WithMany(p => p.OrderFeasibilityCheckedByNavigations)
                .HasForeignKey(d => d.FeasibilityCheckedBy)
                .HasConstraintName("FK_Orders_FeasibilityEngineer");

            entity.HasOne(d => d.Plan).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_ServicePlan");

            entity.HasOne(d => d.Store).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_RetailStore");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");

            entity.HasIndex(e => e.BillId, "IX_Payment_BillID");

            entity.HasIndex(e => e.ReceiptNumber, "UQ_Payment_ReceiptNumber").IsUnique();

            entity.Property(e => e.PaymentId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PaymentID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BillId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("BillID");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("EmployeeID");
            entity.Property(e => e.Notes).HasMaxLength(255);
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReceiptNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ReferenceNumber)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Completed");

            entity.HasOne(d => d.Bill).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_Bill");

            entity.HasOne(d => d.Employee).WithMany(p => p.Payments)
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Payment_Employee");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderId);

            entity.ToTable("PurchaseOrder");

            entity.HasIndex(e => e.Status, "IX_PurchaseOrder_Status");
            entity.HasIndex(e => e.VendorId, "IX_PurchaseOrder_VendorID");

            entity.Property(e => e.PurchaseOrderId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PurchaseOrderID");
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.ExpectedDate).HasColumnType("datetime");
            entity.Property(e => e.ItemCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ItemName).HasMaxLength(150);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReceivedDate).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft");
            entity.Property(e => e.StoreId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StoreID");
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.VendorId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("VendorID");

            entity.HasOne(d => d.Store).WithMany()
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PurchaseOrder_RetailStore");

            entity.HasOne(d => d.Vendor).WithMany()
                .HasForeignKey(d => d.VendorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseOrder_Vendor");
        });

        modelBuilder.Entity<EquipmentRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);

            entity.ToTable("EquipmentRequest");

            entity.HasIndex(e => e.Status, "IX_EquipmentRequest_Status");
            entity.HasIndex(e => e.StoreId, "IX_EquipmentRequest_StoreID");
            entity.HasIndex(e => e.EmployeeId, "IX_EquipmentRequest_EmployeeID");
            entity.HasIndex(e => e.OrderId, "IX_EquipmentRequest_OrderID");

            entity.Property(e => e.RequestId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("RequestID");
            entity.Property(e => e.OrderId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("OrderID");
            entity.Property(e => e.StoreId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StoreID");
            entity.Property(e => e.EmployeeId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("EmployeeID");
            entity.Property(e => e.InventoryId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("InventoryID");
            entity.Property(e => e.ItemName).HasMaxLength(150);
            entity.Property(e => e.DeviceType)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.Urgency)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Normal");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Pending");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.AdminNotes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Order).WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_EquipmentRequest_Orders");

            entity.HasOne(d => d.Store).WithMany()
                .HasForeignKey(d => d.StoreId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EquipmentRequest_RetailStore");

            entity.HasOne(d => d.Employee).WithMany()
                .HasForeignKey(d => d.EmployeeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EquipmentRequest_Employee");

            entity.HasOne(d => d.Inventory).WithMany()
                .HasForeignKey(d => d.InventoryId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_EquipmentRequest_Inventory");
        });

        modelBuilder.Entity<RetailStore>(entity =>
        {
            entity.HasKey(e => e.StoreId);

            entity.ToTable("RetailStore");

            entity.Property(e => e.StoreId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StoreID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CityCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ManagerName).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.OperatingHours).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<ServicePlan>(entity =>
        {
            entity.HasKey(e => e.PlanId);

            entity.ToTable("ServicePlan");

            entity.Property(e => e.PlanId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("PlanID");
            entity.Property(e => e.BillingCycle)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Monthly");
            entity.Property(e => e.CallRates).HasMaxLength(255);
            entity.Property(e => e.ConnectionType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DataLimit)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.HourlyCharge).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MonthlyRental).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.PlanCode)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SecurityDeposit).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.SpeedOrBandwidth)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.Validity)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingKey);

            entity.Property(e => e.SettingKey)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("General");
            entity.Property(e => e.DataType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("string");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.SettingValue).HasMaxLength(255);
        });

        modelBuilder.Entity<Vendor>(entity =>
        {
            entity.ToTable("Vendor");

            entity.HasIndex(e => e.VendorCode, "UQ_Vendor_Code").IsUnique();

            entity.Property(e => e.VendorId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("VendorID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.CompanyName).HasMaxLength(150);
            entity.Property(e => e.ContactPerson).HasMaxLength(100);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Phone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Rating).HasDefaultValue((byte)5);
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.TaxNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.VendorCode)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<VwActiveSubscriber>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_ActiveSubscribers");

            entity.Property(e => e.AccountId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("AccountID");
            entity.Property(e => e.ConnectionStatus)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.ConnectionType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CustomerId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CustomerID");
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CustomerType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DeviceModel).HasMaxLength(150);
            entity.Property(e => e.DeviceSerial)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InstallationAddress).HasMaxLength(255);
            entity.Property(e => e.InstalledDate).HasColumnType("datetime");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.MonthlyRental).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.OrderId)
                .HasMaxLength(11)
                .IsUnicode(false)
                .HasColumnName("OrderID");
            entity.Property(e => e.PlanName).HasMaxLength(150);
            entity.Property(e => e.PortNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RetailStoreName).HasMaxLength(150);
            entity.Property(e => e.SecurityDeposit).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VwBillingSummary>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_BillingSummary");

            entity.Property(e => e.AccountId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("AccountID");
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.BillId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("BillID");
            entity.Property(e => e.BillStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.BillingMonth)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.CustomerType)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DueAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.InvoiceNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LateFeeAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ServiceTaxAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VwInventoryStock>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_InventoryStock");

            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.InventoryId)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("InventoryID");
            entity.Property(e => e.ItemCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ItemName).HasMaxLength(150);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.SupplierName).HasMaxLength(150);
            entity.Property(e => e.TotalStockValue).HasColumnType("decimal(29, 2)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VwRetailStoreStat>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_RetailStoreStats");

            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.CityCode)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ManagerName).HasMaxLength(100);
            entity.Property(e => e.StoreId)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("StoreID");
            entity.Property(e => e.StoreName).HasMaxLength(150);
            entity.Property(e => e.StoreStatus)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

