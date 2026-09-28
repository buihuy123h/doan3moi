import { spawnSync } from 'child_process';

const SERVER = '(localdb)\\MSSQLLocalDB';
const DATABASE = 'NexusSystem';

export function runSqlQuery(sqlQuery) {
  try {
    const input = `SET NOCOUNT ON;\n${sqlQuery}\nGO\n`;
    const res = spawnSync('sqlcmd', ['-S', SERVER, '-d', DATABASE, '-y', '0', '-w', '65535'], {
      input,
      encoding: 'utf8',
      maxBuffer: 20 * 1024 * 1024
    });
    if (res.error) {
      console.error('SQL query execution error:', res.error);
      return null;
    }
    const stdout = (res.stdout || '').replace(/\r?\n/g, '').trim();
    if (!stdout) return null;
    try {
      return JSON.parse(stdout);
    } catch (parseErr) {
      // Fallback: sanitize any corrupted quotes, mojibake, or unescaped characters
      const sanitized = stdout
        .replace(/â€”|â€“|â€[”"]/g, ' - ')
        .replace(/â€/g, '')
        .replace(/[\u201C\u201D]/g, "'")
        .replace(/[\u2018\u2019]/g, "'")
        .replace(/\?\"/g, '"');
      try {
        return JSON.parse(sanitized);
      } catch (innerErr) {
        console.error('SQL query execution failed:', parseErr.message);
        return null;
      }
    }
  } catch (err) {
    console.error('SQL query execution failed:', err.message);
    return null;
  }
}

export function getDatabaseStatus() {
  try {
    const counts = runSqlQuery(`
      SELECT 
        (SELECT COUNT(*) FROM sys.tables) AS tableCount,
        (SELECT COUNT(*) FROM dbo.ServicePlan) AS planCount,
        (SELECT COUNT(*) FROM dbo.Orders) AS orderCount,
        (SELECT COUNT(*) FROM dbo.CustomerConnection) AS connectionCount,
        (SELECT COUNT(*) FROM dbo.RetailStore) AS storeCount,
        (SELECT COUNT(*) FROM dbo.Employee) AS employeeCount,
        (SELECT COUNT(*) FROM dbo.InventoryItem) AS inventoryCount
      FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
    `);

    return {
      status: 'Connected',
      database: DATABASE,
      server: SERVER,
      ...counts,
      timestamp: new Date().toISOString()
    };
  } catch (err) {
    return { status: 'Error', message: err.message };
  }
}

export function getAllDatabaseData() {
  try {
    const plans = runSqlQuery(`
      SELECT 
        PlanID AS id, 
        Name AS name, 
        ConnectionType AS type, 
        SpeedOrBandwidth AS speedOrBandwidth, 
        CAST(MonthlyRental AS float) AS monthlyRental, 
        CAST(HourlyCharge AS float) AS hourlyCharge, 
        CAST(SecurityDeposit AS float) AS securityDeposit, 
        DataLimit AS dataLimit, 
        BillingCycle AS billingCycle, 
        Validity AS validity, 
        IncludedHours AS includedHours, 
        CallRates AS callRates, 
        Description AS description, 
        Status AS status 
      FROM dbo.ServicePlan 
      ORDER BY ConnectionType, MonthlyRental 
      FOR JSON PATH;
    `) || [];

    const retailShops = runSqlQuery(`
      SELECT 
        LOWER(StoreID) AS id, 
        StoreID AS shopCode, 
        Name AS name, 
        City AS city, 
        CityCode AS cityCode, 
        Address AS address, 
        ManagerName AS managerName, 
        Phone AS phone, 
        OperatingHours AS operatingHours, 
        ActiveEmployeesCount AS activeEmployeesCount, 
        TotalSubscribersServed AS totalSubscribersServed 
      FROM dbo.RetailStore 
      FOR JSON PATH;
    `) || [];

    const employees = runSqlQuery(`
      SELECT 
        e.EmployeeID AS id, 
        UPPER(e.EmployeeID) AS employeeCode, 
        e.FullName AS name, 
        e.Email AS email, 
        e.Phone AS phone, 
        e.Role AS role, 
        e.Department AS department, 
        CASE WHEN rs.Name IS NOT NULL THEN rs.Name + ' (' + rs.StoreID + ')' ELSE NULL END AS retailShopAssigned, 
        e.Status AS status, 
        CONVERT(VARCHAR(10), e.DateOfJoining, 120) AS dateOfJoining 
      FROM dbo.Employee e 
      LEFT JOIN dbo.RetailStore rs ON e.StoreID = rs.StoreID 
      FOR JSON PATH;
    `) || [];

    const vendors = runSqlQuery(`
      SELECT 
        VendorID AS id, 
        UPPER(VendorID) AS vendorCode, 
        CompanyName AS companyName, 
        ContactPerson AS contactPerson, 
        Category AS category, 
        Phone AS phone, 
        Email AS email, 
        Address AS address, 
        Rating AS rating, 
        Status AS status 
      FROM dbo.Vendor 
      FOR JSON PATH;
    `) || [];

    const orders = runSqlQuery(`
      SELECT 
        o.OrderID AS id, 
        c.FullName AS customerName, 
        c.ContactNumber AS customerPhone, 
        c.Email AS customerEmail, 
        o.InstallationAddress AS installationAddress, 
        c.IdProofType AS idProofType, 
        c.IdProofNumber AS idProofNumber, 
        p.ConnectionType AS connectionType, 
        o.PlanID AS planId, 
        p.Name AS planName, 
        o.StoreID AS retailOutletCode, 
        e.FullName AS retailEmployeeName, 
        o.RetailApprovedBy AS retailApprovedBy, 
        CONVERT(VARCHAR(16), o.RetailApprovedDate, 120) AS retailApprovedAt, 
        o.RetailApprovalNotes AS retailApprovalNotes, 
        o.AssignedTechnicianName AS assignedTechnician, 
        o.AssignedTechnicianPhone AS assignedTechnicianPhone, 
        CONVERT(VARCHAR(16), o.AssignedTechnicianDate, 120) AS assignedTechnicianDate, 
        CONVERT(VARCHAR(16), o.ApplicationDate, 120) AS createdAt, 
        o.Status AS status, 
        o.FeasibilityNotes AS feasibilityNotes, 
        o.CableDistanceMeters AS cableDistanceMeters, 
        o.DpBoxCapacity AS dpBoxCapacity, 
        CAST(o.SignalLossDbm AS float) AS signalLossDbm, 
        o.AssignedAccountId AS assignedAccountId, 
        o.BulkConnectionsCount AS bulkConnectionsCount, 
        CAST(o.BulkDiscountPercent AS float) AS bulkDiscountPercent, 
        o.ExistingLandlineAccountId AS existingLandlineAccountId, 
        o.LandlineFeasible AS landlineFeasible, 
        o.InternetFeasible AS internetFeasible 
      FROM dbo.Orders o 
      INNER JOIN dbo.Customer c ON o.CustomerID = c.CustomerID 
      INNER JOIN dbo.ServicePlan p ON o.PlanID = p.PlanID 
      LEFT JOIN dbo.Employee e ON o.EmployeeID = e.EmployeeID 
      ORDER BY o.ApplicationDate DESC 
      FOR JSON PATH;
    `) || [];

    const connections = runSqlQuery(`
      SELECT 
        cc.AccountID AS accountId, 
        cc.OrderID AS orderId, 
        c.FullName AS customerName, 
        c.ContactNumber AS customerPhone, 
        c.Email AS customerEmail, 
        o.InstallationAddress AS installationAddress, 
        p.ConnectionType AS connectionType, 
        p.Name AS planName, 
        CAST(p.MonthlyRental AS float) AS monthlyRental, 
        CAST(p.SecurityDeposit AS float) AS securityDeposit, 
        cc.Status AS status, 
        cc.IpAddress AS ipAddress, 
        cc.PortNumber AS portNumber, 
        eq.SerialNumber AS assignedDeviceSerial, 
        eq.DeviceModel AS assignedDeviceModel, 
        CONVERT(VARCHAR(10), cc.InstalledDate, 120) AS installedDate, 
        CONVERT(VARCHAR(16), cc.LastUpdated, 120) AS lastUpdated, 
        cc.LastStatusReason AS lastStatusReason 
      FROM dbo.CustomerConnection cc 
      INNER JOIN dbo.Orders o ON cc.OrderID = o.OrderID 
      INNER JOIN dbo.Customer c ON o.CustomerID = c.CustomerID 
      INNER JOIN dbo.ServicePlan p ON o.PlanID = p.PlanID 
      LEFT JOIN dbo.Equipment eq ON cc.EquipmentID = eq.EquipmentID 
      FOR JSON PATH;
    `) || [];

    const equipments = runSqlQuery(`
      SELECT 
        eq.EquipmentID AS id, 
        eq.SerialNumber AS serialNumber, 
        eq.MacAddress AS macAddress, 
        eq.DeviceModel AS deviceModel, 
        eq.DeviceType AS deviceType, 
        eq.AssignedAccountId AS assignedAccountId, 
        c.FullName AS assignedCustomerName, 
        eq.FirmwareVersion AS firmwareVersion, 
        eq.Status AS status, 
        e.FullName AS assignedTechnician, 
        CONVERT(VARCHAR(10), eq.InstalledDate, 120) AS installedDate 
      FROM dbo.Equipment eq 
      LEFT JOIN dbo.CustomerConnection cc ON eq.AssignedAccountId = cc.AccountID 
      LEFT JOIN dbo.Orders o ON cc.OrderID = o.OrderID 
      LEFT JOIN dbo.Customer c ON o.CustomerID = c.CustomerID 
      LEFT JOIN dbo.Employee e ON eq.AssignedTechnicianId = e.EmployeeID 
      FOR JSON PATH;
    `) || [];

    const bills = runSqlQuery(`
      SELECT 
        b.BillID AS id, 
        b.InvoiceNumber AS invoiceNumber, 
        b.AccountID AS accountId, 
        c.FullName AS customerName, 
        b.BillingMonth AS billingMonth, 
        CONVERT(VARCHAR(10), b.BillingDate, 120) AS billingDate, 
        CONVERT(VARCHAR(10), b.DueDate, 120) AS dueDate, 
        p.Name AS planName, 
        p.ConnectionType AS connectionType, 
        CAST(b.SecurityDeposit AS float) AS securityDeposit, 
        CAST(b.MonthlyRental AS float) AS monthlyRental, 
        CAST(b.HourlyCharges AS float) AS hourlyCharges, 
        CAST(b.DiscountPercent AS float) AS discountPercent, 
        CAST(b.DiscountAmount AS float) AS discountAmount, 
        CAST(b.Subtotal AS float) AS subtotal, 
        CAST(b.ServiceTaxRate AS float) AS serviceTaxRate, 
        CAST(b.ServiceTaxAmount AS float) AS serviceTaxAmount, 
        CAST(b.TotalAmount AS float) AS totalAmount, 
        CAST(b.AmountPaid AS float) AS amountPaid, 
        CAST(b.DueAmount AS float) AS dueAmount, 
        b.Status AS status 
      FROM dbo.Bill b 
      INNER JOIN dbo.CustomerConnection cc ON b.AccountID = cc.AccountID 
      INNER JOIN dbo.Orders o ON cc.OrderID = o.OrderID 
      INNER JOIN dbo.Customer c ON o.CustomerID = c.CustomerID 
      INNER JOIN dbo.ServicePlan p ON o.PlanID = p.PlanID 
      FOR JSON PATH;
    `) || [];

    const feedbacks = runSqlQuery(`
      SELECT 
        f.FeedbackID AS id, 
        f.AccountID AS accountId, 
        f.OrderID AS orderId, 
        f.CustomerName AS customerName, 
        f.Rating AS rating, 
        f.Category AS category, 
        f.Message AS message, 
        CONVERT(VARCHAR(16), f.CreatedAt, 120) AS createdAt, 
        f.Response AS response, 
        e.FullName AS respondedBy, 
        CONVERT(VARCHAR(16), f.RespondedAt, 120) AS respondedAt 
      FROM dbo.Feedback f 
      LEFT JOIN dbo.Employee e ON f.RespondedBy = e.EmployeeID 
      FOR JSON PATH;
    `) || [];

    const inventory = runSqlQuery(`
      SELECT 
        inv.InventoryID AS id, 
        inv.ItemCode AS itemCode, 
        inv.Name AS name, 
        inv.Category AS category, 
        inv.StockQuantity AS stockQuantity, 
        inv.ReorderLevel AS reorderLevel, 
        CAST(inv.UnitCost AS float) AS unitCost, 
        inv.Location AS location, 
        ISNULL(v.CompanyName, 'Cisco Systems Commercial Hardware') AS supplier 
      FROM dbo.InventoryItem inv 
      LEFT JOIN dbo.Vendor v ON inv.VendorID = v.VendorID 
      FOR JSON PATH;
    `) || [];

    return {
      source: 'Microsoft SQL Server [NexusSystem]',
      server: SERVER,
      database: DATABASE,
      status: 'Connected',
      plans,
      retailShops,
      employees,
      vendors,
      inventory,
      orders,
      connections,
      equipments,
      bills,
      feedbacks,
      settings: {
        serviceTaxRate: 12.24,
        latePaymentFeePercent: 5.0,
        defaultSecurityDeposits: {
          Broadband: 500,
          'Dial-Up': 325,
          Landline: 250
        },
        installationGracePeriodDays: 7
      }
    };
  } catch (err) {
    console.error('getAllDatabaseData error:', err);
    return null;
  }
}
