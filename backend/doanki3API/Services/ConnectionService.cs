using System.Data;
using System.Net;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Connections;
using doanki3API.Models;
using doanki3API.Services.Common;
using doanki3API.Services.Email;
using doanki3API.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Services;

public sealed class ConnectionService
{
    private readonly MyAppDbContext _context;
    private readonly AccountIdGenerator _accountIdGenerator;
    private readonly NotificationService _notifications;
    private readonly EmailService _email;

    public ConnectionService(
        MyAppDbContext context,
        AccountIdGenerator accountIdGenerator,
        NotificationService notifications,
        EmailService email)
    {
        _context = context;
        _accountIdGenerator = accountIdGenerator;
        _notifications = notifications;
        _email = email;
    }

    public async Task<IReadOnlyList<ConnectionListItemResponse>> GetConnectionsAsync(
        string? accountId,
        string? status,
        string? equipmentId,
        string? customerName,
        string? phone,
        string? connectionType,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.CustomerConnections.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            var accountIdKey = AccountIdKey(accountId);
            query = query.Where(c => c.AccountId.Replace("-", "").Contains(accountIdKey));
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status.Trim());

        if (!string.IsNullOrWhiteSpace(equipmentId))
            query = query.Where(c => c.EquipmentId == equipmentId.Trim());

        // Advanced search theo spec: tên người dùng kết nối, SĐT đăng ký, loại
        // kết nối (qua Plan), khoảng ngày nhận kết nối (theo InstalledDate).
        if (!string.IsNullOrWhiteSpace(customerName))
            query = query.Where(c => c.Order.Customer.FullName.ToLower().Contains(customerName.Trim().ToLower()));

        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(c => c.Order.Customer.ContactNumber.Contains(phone.Trim()));

        if (!string.IsNullOrWhiteSpace(connectionType))
            query = query.Where(c => c.Order.Plan.ConnectionType == connectionType.Trim());

        if (fromDate.HasValue)
            query = query.Where(c => c.InstalledDate >= fromDate.Value);

        if (toDate.HasValue)
        {
            var endExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(c => c.InstalledDate < endExclusive);
        }

        return await query
            .OrderByDescending(c => c.LastUpdated)
            .Select(c => new ConnectionListItemResponse(
                c.AccountId,
                c.OrderId,
                c.Order.CustomerId,
                c.Order.Customer.FullName,
                c.Order.Customer.ContactNumber,
                c.Order.Customer.Email,
                c.Order.InstallationAddress,
                c.Order.Plan.Name,
                c.Order.Plan.ConnectionType,
                c.Order.Plan.MonthlyRental,
                c.Order.Plan.SecurityDeposit,
                c.EquipmentId,
                c.Equipment == null ? null : c.Equipment.SerialNumber,
                c.Equipment == null ? null : c.Equipment.DeviceModel,
                c.Status,
                c.IpAddress,
                c.PortNumber,
                c.InstalledDate,
                c.SuspensionStartDate,
                c.SuspensionEndDate,
                c.TerminatedDate,
                c.LastUpdated,
                c.LastStatusReason))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<ConnectionListItemResponse>> CreateOrUpdateConnectionAsync(
        CreateOrUpdateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var accountIdValue = Normalize(request.AccountId);
        if (accountIdValue is null)
        {
            return ServiceResult<ConnectionListItemResponse>.Validation(
                "AccountId không được để trống");
        }

        var accountId = NormalizeAccountId(accountIdValue);
        var accountKey = AccountIdKey(accountId);
        if (accountKey.Length != 16 ||
            accountKey[0] is not ('B' or 'D' or 'T') ||
            !accountKey.AsSpan(1).ToString().All(char.IsDigit))
        {
            return ServiceResult<ConnectionListItemResponse>.Validation(
                "AccountId phải gồm tiền tố B/D/T, mã thành phố 3 số và số thứ tự 12 số");
        }

        var status = string.IsNullOrWhiteSpace(request.Status)
            ? ConnectionStatuses.Active
            : ConnectionStatuses.All.FirstOrDefault(item =>
                string.Equals(item, request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null)
        {
            return ServiceResult<ConnectionListItemResponse>.Validation(
                $"Trạng thái không hợp lệ. Các giá trị cho phép: {string.Join(", ", ConnectionStatuses.All)}");
        }

        var ipAddress = Normalize(request.IpAddress);
        if (ipAddress is not null && !IPAddress.TryParse(ipAddress, out _))
        {
            return ServiceResult<ConnectionListItemResponse>.Validation(
                "Địa chỉ IP không hợp lệ");
        }

        var connection = await _context.CustomerConnections
            .Include(c => c.Order)
                .ThenInclude(o => o.Customer)
            .Include(c => c.Order)
                .ThenInclude(o => o.Plan)
            .Include(c => c.Equipment)
                .ThenInclude(equipment => equipment!.Inventory)
            .FirstOrDefaultAsync(c => c.AccountId == accountId, cancellationToken);

        var equipmentId = Normalize(request.EquipmentId);
        var serialNumber = Normalize(request.AssignedDeviceSerial);
        Equipment? requestedEquipment = null;
        if (equipmentId is not null || serialNumber is not null)
        {
            requestedEquipment = await _context.Equipment
                .Include(equipment => equipment.Inventory)
                .FirstOrDefaultAsync(
                    equipment =>
                        (equipmentId != null && equipment.EquipmentId == equipmentId) ||
                        (equipmentId == null && equipment.SerialNumber == serialNumber),
                    cancellationToken);

            if (requestedEquipment is null)
            {
                return ServiceResult<ConnectionListItemResponse>.Validation(
                    "Thiết bị được chỉ định không tồn tại");
            }

            var usedByAnotherConnection = await _context.CustomerConnections.AnyAsync(
                item => item.EquipmentId == requestedEquipment.EquipmentId &&
                        item.AccountId != accountId,
                cancellationToken);
            if (usedByAnotherConnection)
            {
                return ServiceResult<ConnectionListItemResponse>.Conflict(
                    "Thiết bị đã được gán cho kết nối khác");
            }
        }

        var now = DateTime.Now;
        if (connection != null)
        {
            var requestedOrderId = Normalize(request.OrderId);
            if (requestedOrderId is not null && requestedOrderId != connection.OrderId)
            {
                return ServiceResult<ConnectionListItemResponse>.Conflict(
                    "Không thể chuyển kết nối sang đơn hàng khác");
            }

            if (connection.Status == ConnectionStatuses.PermanentlyInactive &&
                status != ConnectionStatuses.PermanentlyInactive)
            {
                return ServiceResult<ConnectionListItemResponse>.Conflict(
                    "Không thể kích hoạt lại kết nối đã ngừng vĩnh viễn");
            }

            var reason = Normalize(request.LastStatusReason);
            if (status != ConnectionStatuses.Active && string.IsNullOrWhiteSpace(reason))
            {
                return ServiceResult<ConnectionListItemResponse>.Validation(
                    "Phải nhập lý do khi ngừng kết nối");
            }

            var oldStatus = connection.Status;
            if (oldStatus != status)
            {
                connection.Status = status;
                connection.LastStatusReason = reason ?? "Synchronized from frontend";
                switch (status)
                {
                    case ConnectionStatuses.TemporarilyInactive:
                        connection.SuspensionStartDate = now;
                        connection.SuspensionEndDate = null;
                        break;
                    case ConnectionStatuses.Active:
                        if (oldStatus == ConnectionStatuses.TemporarilyInactive)
                            connection.SuspensionEndDate = now;
                        break;
                    case ConnectionStatuses.PermanentlyInactive:
                        connection.TerminatedDate = now;
                        ReturnEquipmentToStock(connection, now);
                        break;
                }

                            }

            if (ipAddress is not null)
                connection.IpAddress = ipAddress;
            if (!string.IsNullOrWhiteSpace(request.PortNumber))
                connection.PortNumber = request.PortNumber.Trim();
            if (requestedEquipment is not null && connection.EquipmentId != requestedEquipment.EquipmentId)
            {
                if (connection.EquipmentId is not null)
                {
                    return ServiceResult<ConnectionListItemResponse>.Conflict(
                        "Kết nối đã có thiết bị; hãy thu hồi thiết bị trước khi gán thiết bị khác");
                }

                if (requestedEquipment.Status != EquipmentStatuses.InStock)
                {
                    return ServiceResult<ConnectionListItemResponse>.Conflict(
                        "Thiết bị không ở trạng thái sẵn sàng trong kho");
                }

                AssignEquipment(connection, requestedEquipment, now);
            }
            connection.LastUpdated = now;
        }
        else
        {
            var orderId = Normalize(request.OrderId);
            if (orderId is null)
            {
                return ServiceResult<ConnectionListItemResponse>.Validation(
                    "OrderId là bắt buộc khi tạo kết nối mới");
            }

            var order = await _context.Orders
                .Include(item => item.Customer)
                .Include(item => item.Plan)
                .Include(item => item.Store)
                .FirstOrDefaultAsync(item => item.OrderId == orderId, cancellationToken);
            if (order is null)
                return ServiceResult<ConnectionListItemResponse>.NotFound("Không tìm thấy đơn hàng");
            if (order.Status != OrderStatuses.Feasible)
                return ServiceResult<ConnectionListItemResponse>.Validation("Đơn hàng chưa được xác nhận khả thi");
            if (status != ConnectionStatuses.Active)
                return ServiceResult<ConnectionListItemResponse>.Validation("Kết nối mới phải ở trạng thái Active");
            if (requestedEquipment is null)
                return ServiceResult<ConnectionListItemResponse>.Validation("Phải chọn thiết bị cho kết nối mới");
            if (requestedEquipment.Status != EquipmentStatuses.InStock ||
                !string.IsNullOrWhiteSpace(requestedEquipment.AssignedAccountId))
                return ServiceResult<ConnectionListItemResponse>.Conflict("Thiết bị không sẵn sàng để lắp đặt");
            if (requestedEquipment.StoreId is not null && requestedEquipment.StoreId != order.StoreId)
                return ServiceResult<ConnectionListItemResponse>.Validation("Thiết bị không thuộc cửa hàng xử lý đơn hàng");
            if (requestedEquipment.Inventory is not null && requestedEquipment.Inventory.StockQuantity <= 0)
                return ServiceResult<ConnectionListItemResponse>.Conflict("Mặt hàng kho liên kết đã hết hàng");

            var provisionedCount = await _context.CustomerConnections.CountAsync(
                item => item.OrderId == orderId,
                cancellationToken);
            var requiredCount = Math.Max(1, order.BulkConnectionsCount);
            if (provisionedCount >= requiredCount)
                return ServiceResult<ConnectionListItemResponse>.Conflict("Đơn hàng đã được cấp đủ kết nối");

            var installedDate = request.InstalledDate ?? now;
            if (installedDate > now.AddMinutes(5))
                return ServiceResult<ConnectionListItemResponse>.Validation("Thời gian hoàn công không được ở tương lai");

            connection = new CustomerConnection
            {
                AccountId = accountId,
                OrderId = order.OrderId,
                EquipmentId = requestedEquipment.EquipmentId,
                Status = ConnectionStatuses.Active,
                IpAddress = ipAddress,
                PortNumber = Normalize(request.PortNumber),
                InstalledDate = installedDate,
                LastUpdated = now,
                LastStatusReason = Normalize(request.LastStatusReason) ?? "Connection provided"
            };
            _context.CustomerConnections.Add(connection);
            
            order.AssignedAccountId ??= accountId;
            if (provisionedCount + 1 >= requiredCount)
                order.Status = OrderStatuses.ConnectionProvided;
            order.UpdatedAt = now;
            AssignEquipment(connection, requestedEquipment, now);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var response = await _context.CustomerConnections
            .AsNoTracking()
            .Where(item => item.AccountId == accountId)
            .Select(item => new ConnectionListItemResponse(
                item.AccountId,
                item.OrderId,
                item.Order.CustomerId,
                item.Order.Customer.FullName,
                item.Order.Customer.ContactNumber,
                item.Order.Customer.Email,
                item.Order.InstallationAddress,
                item.Order.Plan.Name,
                item.Order.Plan.ConnectionType,
                item.Order.Plan.MonthlyRental,
                item.Order.Plan.SecurityDeposit,
                item.EquipmentId,
                item.Equipment == null ? null : item.Equipment.SerialNumber,
                item.Equipment == null ? null : item.Equipment.DeviceModel,
                item.Status,
                item.IpAddress,
                item.PortNumber,
                item.InstalledDate,
                item.SuspensionStartDate,
                item.SuspensionEndDate,
                item.TerminatedDate,
                item.LastUpdated,
                item.LastStatusReason))
            .SingleAsync(cancellationToken);

        return ServiceResult<ConnectionListItemResponse>.Success(response);
    }

    public Task<ConnectionDetailResponse?> GetConnectionByIdAsync(
        string accountId,
        CancellationToken cancellationToken)
    {
        var normalizedAccountId = NormalizeAccountId(accountId);
        return _context.CustomerConnections
            .AsNoTracking()
            .Where(c => c.AccountId == normalizedAccountId)
            .Select(c => new ConnectionDetailResponse(
                c.AccountId,
                c.OrderId,
                c.Order.CustomerId,
                c.Order.Customer.FullName,
                c.Order.Customer.ContactNumber,
                c.Order.Customer.Email,
                c.Order.InstallationAddress,
                c.Order.Plan.Name,
                c.Order.Plan.ConnectionType,
                c.Order.Plan.MonthlyRental,
                c.Order.Plan.SecurityDeposit,
                c.EquipmentId,
                c.Equipment == null ? null : c.Equipment.SerialNumber,
                c.Equipment == null ? null : c.Equipment.DeviceModel,
                c.Status,
                c.IpAddress,
                c.PortNumber,
                c.InstalledDate,
                c.SuspensionStartDate,
                c.SuspensionEndDate,
                c.TerminatedDate,
                c.LastUpdated,
                c.LastStatusReason))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<ProvideConnectionResponse>> ProvideConnectionAsync(
        string orderId,
        ProvideConnectionRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var order = await _context.Orders
            .Include(o => o.Plan)
            .Include(o => o.Store)
            // Customer chỉ dùng để lấy tên khách đưa vào nội dung thông báo real-time.
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderId == orderId, cancellationToken);

        if (order is null)
            return ServiceResult<ProvideConnectionResponse>.NotFound("Không tìm thấy đơn hàng");

        if (order.Status != OrderStatuses.Feasible)
        {
            return ServiceResult<ProvideConnectionResponse>.Validation(
                "Đơn hàng chưa được xác nhận khả thi");
        }

        var provisionedConnections = await _context.CustomerConnections.CountAsync(
            c => c.OrderId == orderId,
            cancellationToken);
        var requiredConnections = Math.Max(1, order.BulkConnectionsCount);
        if (provisionedConnections >= requiredConnections)
        {
            return ServiceResult<ProvideConnectionResponse>.Conflict(
                "Đơn hàng này đã được cấp đủ kết nối");
        }

        var equipmentId = Normalize(request.EquipmentId);
        if (equipmentId is null)
        {
            return ServiceResult<ProvideConnectionResponse>.Validation(
                "Phải chọn thiết bị cho kết nối");
        }

        var equipment = await _context.Equipment.FirstOrDefaultAsync(
            e => e.EquipmentId == equipmentId,
            cancellationToken);

        if (equipment is null)
        {
            return ServiceResult<ProvideConnectionResponse>.Validation(
                "Thiết bị không tồn tại");
        }

        var equipmentInUse = !string.IsNullOrWhiteSpace(equipment.AssignedAccountId) ||
            await _context.CustomerConnections.AnyAsync(
                c => c.EquipmentId == equipment.EquipmentId,
                cancellationToken);

        if (equipmentInUse)
        {
            return ServiceResult<ProvideConnectionResponse>.Conflict(
                "Thiết bị đã được gán cho kết nối khác");
        }

        if (equipment.Status != EquipmentStatuses.InStock)
        {
            return ServiceResult<ProvideConnectionResponse>.Validation(
                "Thiết bị hiện không sẵn sàng để lắp đặt");
        }

        if (equipment.StoreId is not null && equipment.StoreId != order.StoreId)
        {
            return ServiceResult<ProvideConnectionResponse>.Validation(
                "Thiết bị không thuộc cửa hàng xử lý đơn hàng");
        }

        if (equipment.InventoryId is not null)
        {
            await _context.Entry(equipment)
                .Reference(item => item.Inventory)
                .LoadAsync(cancellationToken);
            if (equipment.Inventory is not null && equipment.Inventory.StockQuantity <= 0)
            {
                return ServiceResult<ProvideConnectionResponse>.Conflict(
                    "Mặt hàng kho liên kết với thiết bị đã hết hàng");
            }
        }

        var performedBy = Normalize(request.PerformedBy) ??
            order.EmployeeId ??
            order.FeasibilityCheckedBy;

        if (performedBy is not null)
        {
            var employeeExists = await _context.Employees.AnyAsync(
                e => e.EmployeeId == performedBy && e.Status == EmployeeStatuses.Active,
                cancellationToken);

            if (!employeeExists)
            {
                return ServiceResult<ProvideConnectionResponse>.Validation(
                    "Nhân viên thực hiện không tồn tại hoặc đã ngừng hoạt động");
            }
        }

        var now = DateTime.Now;
        var installedDate = request.InstalledDate ?? now;
        if (installedDate > now.AddMinutes(5))
        {
            return ServiceResult<ProvideConnectionResponse>.Validation(
                "Thời gian hoàn công không được ở tương lai");
        }

        var ipAddress = Normalize(request.IpAddress);
        if (ipAddress is not null && !IPAddress.TryParse(ipAddress, out _))
        {
            return ServiceResult<ProvideConnectionResponse>.Validation(
                "Địa chỉ IP không hợp lệ");
        }

        var reservedAccountIdAvailable =
            !string.IsNullOrWhiteSpace(order.AssignedAccountId) &&
            !await _context.CustomerConnections.AnyAsync(
                c => c.AccountId == order.AssignedAccountId,
                cancellationToken);
        var accountId = reservedAccountIdAvailable
            ? order.AssignedAccountId!
            : await _accountIdGenerator.GenerateAsync(
                order.Plan.ConnectionType,
                order.Store.CityCode,
                cancellationToken);

        var connection = new CustomerConnection
        {
            AccountId = accountId,
            OrderId = order.OrderId,
            EquipmentId = equipment.EquipmentId,
            Status = ConnectionStatuses.Active,
            IpAddress = ipAddress,
            PortNumber = Normalize(request.PortNumber),
            InstalledDate = installedDate,
            LastUpdated = now,
            LastStatusReason = "Connection provided"
        };

        _context.CustomerConnections.Add(connection);
        
        order.AssignedAccountId ??= accountId;
        var totalProvisioned = provisionedConnections + 1;
        if (totalProvisioned >= requiredConnections)
            order.Status = OrderStatuses.ConnectionProvided;
        order.UpdatedAt = now;

        equipment.AssignedAccountId = accountId;
        equipment.AssignedTechnicianId = performedBy;
        equipment.InstalledDate = installedDate;
        equipment.Status = EquipmentStatuses.InService;
        equipment.UpdatedAt = now;
        if (equipment.Inventory is not null)
        {
            equipment.Inventory.StockQuantity--;
            equipment.Inventory.Status = InventoryStatusFor(
                equipment.Inventory.StockQuantity,
                equipment.Inventory.ReorderLevel);
            equipment.Inventory.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Đã đấu nối xong -> kế toán lập hoá đơn, khách nhận tin báo đường truyền sẵn sàng.
        await _notifications.ConnectionProvidedAsync(
            connection.AccountId,
            order.Customer?.FullName ?? "Khách hàng",
            order.Plan?.Name,
            cancellationToken);

        await _notifications.NotifyDataChangedAsync("connections", connection.AccountId, cancellationToken);

        // Gửi Mã tài khoản cho khách ngay khi đấu nối xong — đây là mã để đăng nhập cổng
        // khách hàng. Chạy nền: mail chậm/lỗi không được làm chậm hay hỏng thao tác kỹ thuật
        // (EmailService tự nuốt lỗi và ghi log).
        var customerEmail = order.Customer?.Email;
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var customerName = string.IsNullOrWhiteSpace(order.Customer?.FullName)
                ? "Quý khách"
                : order.Customer!.FullName;
            var mailAccountId = connection.AccountId;
            var planName = order.Plan?.Name;
            var provisionedOrderId = order.OrderId;

            _ = Task.Run(() => _email.SendConnectionReadyAsync(
                customerEmail, customerName, mailAccountId, provisionedOrderId, planName, CancellationToken.None));
        }

        return ServiceResult<ProvideConnectionResponse>.Success(
            new ProvideConnectionResponse(
                "Cung cấp kết nối thành công",
                order.OrderId,
                connection.AccountId,
                connection.Status,
                connection.EquipmentId,
                connection.InstalledDate,
                totalProvisioned,
                requiredConnections,
                order.Status));
    }

    public async Task<ServiceResult<UpdateConnectionStatusResponse>> UpdateStatusAsync(
        string accountId,
        UpdateConnectionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedAccountId = NormalizeAccountId(accountId);
        var connection = await _context.CustomerConnections
            .Include(c => c.Equipment)
                .ThenInclude(equipment => equipment!.Inventory)
            .FirstOrDefaultAsync(c => c.AccountId == normalizedAccountId, cancellationToken);

        if (connection is null)
        {
            return ServiceResult<UpdateConnectionStatusResponse>.NotFound(
                "Không tìm thấy kết nối");
        }

        var requestedStatus = request.Status.Trim();
        var normalizedStatus = ConnectionStatuses.All.FirstOrDefault(
            status => string.Equals(
                status,
                requestedStatus,
                StringComparison.OrdinalIgnoreCase));

        if (normalizedStatus is null)
        {
            return ServiceResult<UpdateConnectionStatusResponse>.Validation(
                $"Trạng thái không hợp lệ. Các giá trị cho phép: {string.Join(", ", ConnectionStatuses.All)}");
        }

        if (normalizedStatus != ConnectionStatuses.Active &&
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return ServiceResult<UpdateConnectionStatusResponse>.Validation(
                "Phải nhập lý do khi ngừng kết nối");
        }

        if (connection.Status == ConnectionStatuses.PermanentlyInactive &&
            normalizedStatus != ConnectionStatuses.PermanentlyInactive)
        {
            return ServiceResult<UpdateConnectionStatusResponse>.Conflict(
                "Không thể kích hoạt lại kết nối đã ngừng vĩnh viễn");
        }

        var performedBy = Normalize(request.PerformedBy);
        if (performedBy is not null)
        {
            var employeeExists = await _context.Employees.AnyAsync(
                e => e.EmployeeId == performedBy && e.Status == EmployeeStatuses.Active,
                cancellationToken);

            if (!employeeExists)
            {
                return ServiceResult<UpdateConnectionStatusResponse>.Validation(
                    "Nhân viên thực hiện không tồn tại hoặc đã ngừng hoạt động");
            }
        }

        var oldStatus = connection.Status;
        var now = DateTime.Now;
        connection.Status = normalizedStatus;
        connection.LastStatusReason = Normalize(request.Reason);
        connection.LastUpdated = now;

        switch (normalizedStatus)
        {
            case ConnectionStatuses.TemporarilyInactive:
                connection.SuspensionStartDate = now;
                connection.SuspensionEndDate = null;
                break;
            case ConnectionStatuses.Active:
                if (oldStatus == ConnectionStatuses.TemporarilyInactive)
                    connection.SuspensionEndDate = now;
                break;
            case ConnectionStatuses.PermanentlyInactive:
                connection.TerminatedDate = now;
                if (connection.Equipment is not null)
                {
                    connection.Equipment.AssignedAccountId = null;
                    connection.Equipment.AssignedTechnicianId = null;
                    connection.Equipment.InstalledDate = null;
                    connection.Equipment.Status = EquipmentStatuses.InStock;
                    connection.Equipment.UpdatedAt = now;
                    if (connection.Equipment.Inventory is not null &&
                        connection.Equipment.Inventory.StockQuantity < int.MaxValue)
                    {
                        connection.Equipment.Inventory.StockQuantity++;
                        connection.Equipment.Inventory.Status = InventoryStatusFor(
                            connection.Equipment.Inventory.StockQuantity,
                            connection.Equipment.Inventory.ReorderLevel);
                        connection.Equipment.Inventory.UpdatedAt = now;
                    }
                    connection.EquipmentId = null;
                }
                break;
        }

        
        await _context.SaveChangesAsync(cancellationToken);

        if (oldStatus != connection.Status)
        {
            var subscriberName = await _context.Orders
                .Where(o => o.OrderId == connection.OrderId)
                .Select(o => o.Customer.FullName)
                .FirstOrDefaultAsync(cancellationToken);

            await _notifications.ConnectionStatusChangedAsync(
                connection.AccountId,
                subscriberName ?? "Khách hàng",
                connection.Status,
                connection.LastStatusReason,
                cancellationToken);

            await _notifications.NotifyDataChangedAsync("connections", connection.AccountId, cancellationToken);
        }

        return ServiceResult<UpdateConnectionStatusResponse>.Success(
            new UpdateConnectionStatusResponse(
                "Cập nhật trạng thái kết nối thành công",
                connection.AccountId,
                oldStatus,
                connection.Status));
    }

    

    private static void AssignEquipment(
        CustomerConnection connection,
        Equipment equipment,
        DateTime timestamp)
    {
        connection.EquipmentId = equipment.EquipmentId;
        connection.Equipment = equipment;
        equipment.AssignedAccountId = connection.AccountId;
        equipment.InstalledDate = connection.InstalledDate;
        equipment.Status = EquipmentStatuses.InService;
        equipment.UpdatedAt = timestamp;

        if (equipment.Inventory is not null)
        {
            equipment.Inventory.StockQuantity--;
            equipment.Inventory.Status = InventoryStatusFor(
                equipment.Inventory.StockQuantity,
                equipment.Inventory.ReorderLevel);
            equipment.Inventory.UpdatedAt = timestamp;
        }
    }

    private static void ReturnEquipmentToStock(
        CustomerConnection connection,
        DateTime timestamp)
    {
        if (connection.Equipment is null)
            return;

        connection.Equipment.AssignedAccountId = null;
        connection.Equipment.AssignedTechnicianId = null;
        connection.Equipment.InstalledDate = null;
        connection.Equipment.Status = EquipmentStatuses.InStock;
        connection.Equipment.UpdatedAt = timestamp;
        if (connection.Equipment.Inventory is not null &&
            connection.Equipment.Inventory.StockQuantity < int.MaxValue)
        {
            connection.Equipment.Inventory.StockQuantity++;
            connection.Equipment.Inventory.Status = InventoryStatusFor(
                connection.Equipment.Inventory.StockQuantity,
                connection.Equipment.Inventory.ReorderLevel);
            connection.Equipment.Inventory.UpdatedAt = timestamp;
        }

        connection.EquipmentId = null;
        connection.Equipment = null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string AccountIdKey(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static string NormalizeAccountId(string value)
    {
        var key = AccountIdKey(value);
        return key.Length > 4
            ? $"{key[..4]}-{key[4..]}"
            : key;
    }

    private static string InventoryStatusFor(int stockQuantity, int reorderLevel) =>
        stockQuantity == 0
            ? InventoryStatuses.OutOfStock
            : stockQuantity <= reorderLevel
                ? InventoryStatuses.LowStock
                : InventoryStatuses.InStock;
}


