using System.Data;
using doanki3API.Constants;
using doanki3API.Data;
using doanki3API.DTOs.Orders;
using doanki3API.Services.Common;
using doanki3API.Services.Email;
using doanki3API.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using doanki3API.Models;

namespace doanki3API.Services;

public sealed class TechnicalOrderService
{
    private readonly MyAppDbContext _context;
    private readonly AccountIdGenerator _accountIdGenerator;
    private readonly EmailService _email;
    private readonly NotificationService _notifications;

    public TechnicalOrderService(
        MyAppDbContext context,
        AccountIdGenerator accountIdGenerator,
        EmailService email,
        NotificationService notifications)
    {
        _email = email;
        _context = context;
        _accountIdGenerator = accountIdGenerator;
        _notifications = notifications;
    }

    public async Task<IReadOnlyList<OrderListItemResponse>> GetOrdersAsync(
        string? status,
        string? storeId,
        string? keyword,
        string? customerName,
        string? phone,
        string? connectionType,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var query = _context.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status.Trim());

        if (!string.IsNullOrWhiteSpace(storeId))
            query = query.Where(o => o.StoreId == storeId.Trim());

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var value = keyword.Trim();
            query = query.Where(o =>
                o.OrderId.Contains(value) ||
                o.CustomerId.Contains(value) ||
                o.InstallationAddress.Contains(value));
        }

        // Advanced search theo spec: tên người đặt đơn, số điện thoại đăng ký,
        // loại kết nối, khoảng ngày nộp đơn (from/to gồm cả biên "đến").
        if (!string.IsNullOrWhiteSpace(customerName))
            query = query.Where(o => o.Customer.FullName.ToLower().Contains(customerName.Trim().ToLower()));

        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(o => o.Customer.ContactNumber.Contains(phone.Trim()));

        if (!string.IsNullOrWhiteSpace(connectionType))
            query = query.Where(o => o.Plan.ConnectionType == connectionType.Trim());

        if (fromDate.HasValue)
            query = query.Where(o => o.ApplicationDate >= fromDate.Value);

        if (toDate.HasValue)
        {
            // "Đến 2026-09-26" nghĩa là lấy mọi đơn nộp trong ngày 26.
            var endExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(o => o.ApplicationDate < endExclusive);
        }

        return await query
            .OrderByDescending(o => o.ApplicationDate)
            .Select(o => new OrderListItemResponse(
                o.OrderId,
                o.CustomerId,
                o.Customer.FullName,
                o.Customer.ContactNumber,
                o.Customer.Email,
                o.PlanId,
                o.Plan.Name,
                o.Plan.ConnectionType,
                o.StoreId,
                o.Store.Name,
                o.InstallationAddress,
                o.ApplicationDate,
                o.Status,
                o.CableDistanceMeters,
                o.DpBoxCapacity,
                o.SignalLossDbm,
                o.FeasibilityNotes,
                o.RejectionReason,
                o.InternetFeasible,
                o.FeasibilityCheckedBy,
                o.FeasibilityCheckedDate,
                o.ScheduledInstallDate,
                o.AssignedAccountId,
                o.CustomerConnections.Count,
                o.BulkConnectionsCount,
                o.BulkDiscountPercent,
                o.ExistingLandlineAccountId,
                o.LandlineFeasible,
                o.RetailApprovedBy,
                o.RetailApprovedDate,
                o.RetailApprovalNotes,
                o.AssignedTechnicianName,
                o.AssignedTechnicianPhone,
                o.AssignedTechnicianDate,
                o.EmployeeId))
            .ToListAsync(cancellationToken);
    }

    public Task<OrderDetailResponse?> GetOrderByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderId == id)
            .Select(o => new OrderDetailResponse(
                o.OrderId,
                o.CustomerId,
                o.Customer.FullName,
                o.Customer.ContactNumber,
                o.Customer.Email,
                o.PlanId,
                o.Plan.Name,
                o.Plan.ConnectionType,
                o.StoreId,
                o.Store.Name,
                o.Store.CityCode,
                o.EmployeeId,
                o.InstallationAddress,
                o.ApplicationDate,
                o.Status,
                o.CableDistanceMeters,
                o.DpBoxCapacity,
                o.SignalLossDbm,
                o.FeasibilityNotes,
                o.FeasibilityCheckedBy,
                o.FeasibilityCheckedDate,
                o.ScheduledInstallDate,
                o.RejectionReason,
                o.InternetFeasible,
                o.LandlineFeasible,
                o.AssignedAccountId,
                o.CustomerConnections.Count,
                o.BulkConnectionsCount,
                o.BulkDiscountPercent,
                o.CreatedAt,
                o.UpdatedAt,
                o.RetailApprovedBy,
                o.RetailApprovedDate,
                o.RetailApprovalNotes,
                o.AssignedTechnicianName,
                o.AssignedTechnicianPhone,
                o.AssignedTechnicianDate))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<OrderStatusResponse>> UpdateFeasibilityAsync(
        string id,
        FeasibilityRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var order = await _context.Orders
            .Include(o => o.Plan)
            .Include(o => o.Store)
            .FirstOrDefaultAsync(o => o.OrderId == id, cancellationToken);

        if (order is null)
            return ServiceResult<OrderStatusResponse>.NotFound("Không tìm thấy đơn hàng");

        if (order.Status == OrderStatuses.ConnectionProvided)
        {
            return ServiceResult<OrderStatusResponse>.Conflict(
                "Đơn hàng đã được cấp đủ kết nối, không thể đánh giá lại");
        }

        if (await _context.CustomerConnections.AnyAsync(
            connection => connection.OrderId == order.OrderId,
            cancellationToken))
        {
            return ServiceResult<OrderStatusResponse>.Conflict(
                "Đơn hàng đã có kết nối, không thể đánh giá lại");
        }

        if (!request.IsFeasible && string.IsNullOrWhiteSpace(request.RejectionReason))
        {
            return ServiceResult<OrderStatusResponse>.Validation(
                "Đơn không khả thi phải có lý do");
        }

        var checkedBy = request.CheckedBy.Trim();
        var employeeExists = await _context.Employees.AnyAsync(
            e => e.EmployeeId == checkedBy && e.Status == EmployeeStatuses.Active,
            cancellationToken);

        if (!employeeExists)
        {
            return ServiceResult<OrderStatusResponse>.Validation(
                "Nhân viên kiểm tra không tồn tại hoặc đã ngừng hoạt động");
        }

        var now = DateTime.Now;
        var connectionType = order.Plan.ConnectionType.Trim().ToUpperInvariant();
        if (connectionType is "DIAL-UP" or "DIALUP" && request.IsFeasible)
        {
            var landlinePassed = !string.IsNullOrWhiteSpace(order.ExistingLandlineAccountId) ||
                request.LandlineFeasible == true;
            if (!landlinePassed || request.InternetFeasible != true)
            {
                return ServiceResult<OrderStatusResponse>.Validation(
                    "Đơn Dial-Up phải đạt cả nhánh Landline và Internet");
            }

            order.LandlineFeasible = true;
            order.InternetFeasible = true;
        }
        else if (connectionType is "TELEPHONE" or "LANDLINE")
            order.LandlineFeasible = request.IsFeasible;
        else
            order.InternetFeasible = request.IsFeasible;

        order.FeasibilityNotes = Normalize(request.Notes);
        order.CableDistanceMeters = request.CableDistanceMeters ?? order.CableDistanceMeters;
        order.DpBoxCapacity = Normalize(request.DpBoxCapacity) ?? order.DpBoxCapacity;
        order.SignalLossDbm = request.SignalLossDbm ?? order.SignalLossDbm;
        order.FeasibilityCheckedBy = checkedBy;
        order.FeasibilityCheckedDate = now;
        order.UpdatedAt = now;
        order.Status = request.IsFeasible
            ? OrderStatuses.Feasible
            : OrderStatuses.NotFeasible;
        order.RejectionReason = request.IsFeasible
            ? null
            : request.RejectionReason!.Trim();

        if (request.IsFeasible && string.IsNullOrWhiteSpace(order.AssignedAccountId))
        {
            order.AssignedAccountId = await _accountIdGenerator.GenerateAsync(
                order.Plan.ConnectionType,
                order.Store.CityCode,
                cancellationToken);
        }
        else if (!request.IsFeasible)
        {
            order.AssignedAccountId = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await _notifications.OrderFeasibilityAsync(
            order.OrderId,
            request.IsFeasible,
            order.AssignedAccountId,
            order.RejectionReason ?? order.FeasibilityNotes,
            cancellationToken);

        await _notifications.NotifyDataChangedAsync("orders", order.OrderId, cancellationToken);

        var message = request.IsFeasible
            ? "Đã xác nhận đơn khả thi"
            : "Đã xác nhận đơn không khả thi";

        return ServiceResult<OrderStatusResponse>.Success(
            new OrderStatusResponse(
                message,
                order.OrderId,
                order.Status,
                order.AssignedAccountId));
    }

    public async Task<ServiceResult<InstallationScheduleResponse>> UpdateScheduleAsync(
        string id,
        UpdateInstallationScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderId == id, cancellationToken);

        if (order is null)
        {
            return ServiceResult<InstallationScheduleResponse>.NotFound(
                "Không tìm thấy đơn hàng");
        }

        if (order.Status != OrderStatuses.Feasible)
        {
            return ServiceResult<InstallationScheduleResponse>.Validation(
                "Chỉ có thể lên lịch cho đơn hàng khả thi");
        }

        if (request.ScheduledInstallDate <= DateTime.Now)
        {
            return ServiceResult<InstallationScheduleResponse>.Validation(
                "Thời gian lắp đặt phải lớn hơn thời gian hiện tại");
        }

        var technicianId = Normalize(request.TechnicianId);
        if (technicianId is not null)
        {
            var technicianExists = await _context.Employees.AnyAsync(
                e => e.EmployeeId == technicianId && e.Status == EmployeeStatuses.Active,
                cancellationToken);

            if (!technicianExists)
            {
                return ServiceResult<InstallationScheduleResponse>.Validation(
                    "Kỹ thuật viên không tồn tại hoặc đã ngừng hoạt động");
            }
        }

        order.ScheduledInstallDate = request.ScheduledInstallDate;
        order.EmployeeId = technicianId ?? order.EmployeeId;
        order.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<InstallationScheduleResponse>.Success(
            new InstallationScheduleResponse(
                "Lên lịch lắp đặt thành công",
                order.OrderId,
                order.ScheduledInstallDate,
                order.EmployeeId,
                order.Status));
    }

    public async Task<OrderMutationResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var orderId = Normalize(request.OrderId) ?? await GenerateOrderIdAsync(request.ConnectionType, cancellationToken);

        var email = Normalize(request.CustomerEmail);
        var phone = Normalize(request.CustomerPhone);

        // Khách có thể đã có hồ sơ từ trước: đăng ký tài khoản ở trang /register,
        // hoặc từng đặt đơn lần trước. Tìm lại theo email rồi tới số điện thoại để
        // không sinh ra nhiều bản ghi Customer cho cùng một người.
        Customer? customer = null;
        if (email is not null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        }

        if (customer is null && phone is not null)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ContactNumber == phone, cancellationToken);
        }

        if (customer is null)
        {
            customer = new Customer
            {
                CustomerId = $"CUST-{DateTime.UtcNow.Ticks.ToString().Substring(10)}",
                FullName = request.CustomerName!,
                ContactNumber = phone!,
                Email = email,
                Address = request.InstallationAddress!,
                IdProofType = request.IdProofType!,
                IdProofNumber = request.IdProofNumber!,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
        }
        else
        {
            // Hồ sơ tạo lúc đăng ký tài khoản chưa có địa chỉ và giấy tờ tuỳ thân —
            // bổ sung từ đơn hàng này, nhưng không ghi đè dữ liệu khách đã khai trước đó.
            if (string.IsNullOrWhiteSpace(customer.Address))
                customer.Address = request.InstallationAddress!;

            if (!string.IsNullOrWhiteSpace(request.IdProofType))
                customer.IdProofType = request.IdProofType!;

            if (!string.IsNullOrWhiteSpace(request.IdProofNumber))
                customer.IdProofNumber = request.IdProofNumber!;

            customer.UpdatedAt = DateTime.UtcNow;
        }

        var customerId = customer.CustomerId;

        var order = new Order
        {
            OrderId = orderId,
            CustomerId = customerId,
            InstallationAddress = request.InstallationAddress ?? "",
            ApplicationDate = DateTime.UtcNow,
            // Trạng thái lạ sẽ bị CK_Orders_Status chặn ở tầng CSDL -> quy về Pending cho an toàn.
            Status = OrderStatuses.All.FirstOrDefault(
                value => string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? OrderStatuses.Pending,
            PlanId = request.PlanId ?? "default-plan",
            StoreId = request.RetailOutletCode ?? "default-store",
            EmployeeId = request.AssignedEmployeeId,
            // Server tự tính theo bậc bulk của spec — không tin con số client gửi.
            BulkConnectionsCount = Math.Max(1, request.BulkConnectionsCount),
            BulkDiscountPercent = BulkDiscounts.Compute(Math.Max(1, request.BulkConnectionsCount)),
            ExistingLandlineAccountId = Normalize(request.ExistingLandlineAccountId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        // Gửi mã đơn hàng về email khách. Đơn đã lưu xong trước bước này, nên mail hỏng
        // cũng không làm mất đơn — EmailService tự nuốt lỗi và chỉ ghi log.
        if (!string.IsNullOrWhiteSpace(email))
        {
            _ = Task.Run(() => _email.SendOrderPlacedAsync(
                email,
                customer.FullName,
                order.OrderId,
                Normalize(request.PlanName),
                CancellationToken.None));
        }

        // Thông báo real-time cho khâu sẽ xử lý đơn này: bán lẻ nếu còn phải duyệt hồ sơ,
        // kỹ thuật nếu đơn vào thẳng hàng đợi khảo sát.
        await _notifications.OrderPlacedAsync(
            order.OrderId,
            customer.FullName,
            Normalize(request.PlanName),
            needsRetailApproval: order.Status == OrderStatuses.PendingRetail,
            cancellationToken);

        await _notifications.NotifyDataChangedAsync("orders", order.OrderId, cancellationToken);

        return ToMutationResponse(order);
    }

    /// <summary>
    /// Đổi trạng thái đơn hàng, dùng chung cho cả 3 khâu:
    /// Cửa hàng bán lẻ duyệt/từ chối hồ sơ, phân công kỹ thuật viên khảo sát,
    /// và Kỹ thuật kết luận khả thi.
    /// </summary>
    public async Task<ServiceResult<OrderMutationResponse>> UpdateOrderStatusAsync(
        string id,
        UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        // Chặn trạng thái lạ ngay tại tầng service, nếu không CSDL sẽ chặn bằng
        // CK_Orders_Status và trả về lỗi 500 khó hiểu cho người dùng.
        var status = OrderStatuses.All.FirstOrDefault(
            value => string.Equals(value, request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));

        if (status is null)
        {
            return ServiceResult<OrderMutationResponse>.Validation(
                $"Trạng thái không hợp lệ. Các giá trị cho phép: {string.Join(", ", OrderStatuses.All)}");
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id, cancellationToken);
        if (order == null)
        {
            return ServiceResult<OrderMutationResponse>.NotFound("Không tìm thấy đơn hàng");
        }

        var now = DateTime.UtcNow;
        order.Status = status;
        order.UpdatedAt = now;

        if (status == OrderStatuses.NotApproved)
        {
            // Cửa hàng bán lẻ từ chối hồ sơ — lý do là bắt buộc để khách biết vì sao.
            var reason = Normalize(request.RetailRejectionReason) ?? Normalize(request.Reason);
            if (reason is null)
            {
                return ServiceResult<OrderMutationResponse>.Validation(
                    "Phải nhập lý do khi từ chối hồ sơ của khách.");
            }

            order.RejectionReason = reason;
        }
        else if (status == OrderStatuses.Feasible || status == OrderStatuses.NotFeasible)
        {
            order.FeasibilityNotes = Normalize(request.Reason) ?? order.FeasibilityNotes;
        }

        // Lưu vết người xử lý hồ sơ tại cửa hàng bán lẻ — dùng chung cho cả duyệt lẫn từ chối.
        var handledBy = Normalize(request.RetailApprovedBy);
        if (handledBy is not null)
        {
            order.RetailApprovedBy = handledBy;
            order.RetailApprovedDate = now;
            order.RetailApprovalNotes = Normalize(request.RetailApprovalNotes) ?? order.RetailApprovalNotes;
        }

        if (status == OrderStatuses.PendingRetail)
        {
            // Hồ sơ bị trả lại rồi nộp lại -> xoá hết dấu vết lần xử lý trước
            // để đơn quay về đúng trạng thái "chờ duyệt" sạch sẽ.
            order.RetailApprovedBy = null;
            order.RetailApprovedDate = null;
            order.RetailApprovalNotes = null;
            order.RejectionReason = null;
        }
        else if (status != OrderStatuses.NotApproved)
        {
            if (request.RetailApprovalNotes is not null)
            {
                order.RetailApprovalNotes = Normalize(request.RetailApprovalNotes);
            }

            // Đã duyệt thì lý do từ chối của lần trước không còn đúng nữa.
            // Chỉ xoá khi đúng là thao tác duyệt (có tên người duyệt), tránh xoá nhầm
            // khi endpoint được gọi cho việc khác như phân công kỹ thuật viên.
            if (handledBy is not null)
            {
                order.RejectionReason = null;
            }
        }

        // Phân công kỹ thuật viên đi khảo sát.
        var technicianName = Normalize(request.AssignedTechnician);
        var technicianId = Normalize(request.AssignedTechnicianId);

        if (technicianName is not null || technicianId is not null)
        {
            order.AssignedTechnicianName = technicianName ?? order.AssignedTechnicianName;
            order.AssignedTechnicianPhone = Normalize(request.AssignedTechnicianPhone)
                ?? order.AssignedTechnicianPhone;
            order.AssignedTechnicianDate = now;

            if (technicianId is not null)
            {
                // Orders.EmployeeID có khoá ngoại sang Employee. Mã không có thật sẽ làm
                // SaveChanges ném lỗi FK, nên chỉ gán khi nhân viên thực sự tồn tại;
                // tên và số điện thoại vẫn được lưu để giao diện hiển thị đúng người.
                var employeeExists = await _context.Employees.AnyAsync(
                    e => e.EmployeeId == technicianId, cancellationToken);

                if (employeeExists)
                {
                    order.EmployeeId = technicianId;
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Báo ngay cho khâu kế tiếp trong quy trình, không bắt họ phải bấm tải lại trang.
        if (status == OrderStatuses.PendingRetail)
        {
            await _notifications.OrderResubmittedAsync(order.OrderId, cancellationToken);
        }
        else if (status == OrderStatuses.NotApproved)
        {
            await _notifications.OrderRejectedByRetailAsync(
                order.OrderId, order.RejectionReason ?? "", order.AssignedAccountId, cancellationToken);
        }
        else if (status == OrderStatuses.Pending && handledBy is not null)
        {
            // Có tên người duyệt -> đây đúng là thao tác duyệt hồ sơ của cửa hàng bán lẻ.
            await _notifications.OrderApprovedByRetailAsync(order.OrderId, handledBy, cancellationToken);
        }
        else if (status == OrderStatuses.Feasible || status == OrderStatuses.NotFeasible)
        {
            await _notifications.OrderFeasibilityAsync(
                order.OrderId,
                status == OrderStatuses.Feasible,
                order.AssignedAccountId,
                order.RejectionReason ?? order.FeasibilityNotes,
                cancellationToken);
        }

        if (technicianName is not null)
        {
            await _notifications.TechnicianAssignedAsync(order.OrderId, technicianName, cancellationToken);
        }

        await _notifications.NotifyDataChangedAsync("orders", order.OrderId, cancellationToken);

        return ServiceResult<OrderMutationResponse>.Success(ToMutationResponse(order));
    }

    /// <summary>
    /// Sinh mã đơn đúng định dạng CK_Orders_ID_Format: 11 ký tự gồm 1 chữ cái
    /// loại kết nối (D/B/T) + 10 chữ số tăng dần. Chỉ dùng khi frontend không tự gửi mã.
    /// </summary>
    private async Task<string> GenerateOrderIdAsync(string? connectionType, CancellationToken cancellationToken)
    {
        var prefix = connectionType?.Trim().ToUpperInvariant() switch
        {
            "DIAL-UP" => "D",
            "LANDLINE" => "T",
            "TELEPHONE" => "T",
            _ => "B"
        };

        // Spec: "Alpha numeric code in serial order" — serial tăng chung TOÀN HỆ
        // THỐNG, không reset theo loại (VD đơn Dial-Up đầu có thể là D0000000003
        // nếu B0000000001, T0000000002 đã tồn tại).
        var existing = await _context.Orders
            .Select(o => o.OrderId)
            .ToListAsync(cancellationToken);

        var maxSerial = existing
            .Select(id => long.TryParse(id.Length == 11 ? id[1..] : null, out var value) ? value : 0L)
            .DefaultIfEmpty(0L)
            .Max();

        return $"{prefix}{(maxSerial + 1):D10}";
    }

    /// <summary>Chuyển entity Order sang DTO phẳng để trả về cho frontend.</summary>
    private static OrderMutationResponse ToMutationResponse(Order order) => new(
        order.OrderId,
        order.CustomerId,
        order.PlanId,
        order.StoreId,
        order.EmployeeId,
        order.InstallationAddress,
        order.Status,
        order.FeasibilityNotes,
        order.RejectionReason,
        order.RetailApprovedBy,
        order.RetailApprovedDate,
        order.RetailApprovalNotes,
        order.AssignedTechnicianName,
        order.AssignedTechnicianPhone,
        order.AssignedTechnicianDate,
        order.AssignedAccountId,
        order.ScheduledInstallDate,
        order.ApplicationDate,
        order.CreatedAt,
        order.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
