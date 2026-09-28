using doanki3API.Data;
using doanki3API.Constants;
using doanki3API.DTOs.Equipment;
using doanki3API.Models;
using doanki3API.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Services;

public sealed class EquipmentService
{
    private readonly MyAppDbContext _context;

    public EquipmentService(MyAppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EquipmentResponse>> GetEquipmentAsync(
        string? status,
        string? storeId,
        bool availableOnly,
        CancellationToken cancellationToken)
    {
        var query = _context.Equipment.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(equipment => equipment.Status == status.Trim());

        if (!string.IsNullOrWhiteSpace(storeId))
            query = query.Where(equipment => equipment.StoreId == storeId.Trim());

        if (availableOnly)
        {
            query = query.Where(equipment =>
                equipment.Status == EquipmentStatuses.InStock &&
                equipment.AssignedAccountId == null &&
                equipment.CustomerConnection == null);
        }

        return await Project(query.OrderBy(equipment => equipment.EquipmentId))
            .ToListAsync(cancellationToken);
    }

    public Task<EquipmentResponse?> GetEquipmentByIdAsync(
        string id,
        CancellationToken cancellationToken)
    {
        return Project(_context.Equipment.AsNoTracking().Where(equipment => equipment.EquipmentId == id))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ServiceResult<EquipmentMutationResponse>> CreateEquipmentAsync(
        CreateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var equipmentId = Normalize(request.EquipmentId);
        var deviceModel = Normalize(request.DeviceModel);
        var deviceType = Normalize(request.DeviceType);
        var vendorId = Normalize(request.VendorId);
        var status = NormalizeStatus(request.Status);

        if (equipmentId is null || deviceModel is null ||
            deviceType is null || vendorId is null)
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                "EquipmentId, DeviceModel, DeviceType và VendorId không được để trống");
        }

        if (status is null)
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                $"Trạng thái không hợp lệ. Các giá trị cho phép: {string.Join(", ", EquipmentStatuses.All)}");
        }

        if (await _context.Equipment.AnyAsync(
            equipment => equipment.EquipmentId == equipmentId,
            cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Conflict(
                "EquipmentId đã tồn tại");
        }

        var serialNumber = Normalize(request.SerialNumber);
        var macAddress = Normalize(request.MacAddress);
        if (serialNumber is null || macAddress is null)
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                "SerialNumber và MacAddress không được để trống");
        }

        if (await _context.Equipment.AnyAsync(
                equipment => equipment.SerialNumber == serialNumber,
                cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Conflict(
                "Serial number đã tồn tại");
        }

        if (await _context.Equipment.AnyAsync(
            equipment => equipment.MacAddress == macAddress,
            cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Conflict(
                "Địa chỉ MAC đã tồn tại");
        }

        if (!await _context.Vendors.AnyAsync(
            vendor => vendor.VendorId == vendorId,
            cancellationToken))
        {
            vendorId = (await _context.Vendors.Select(v => v.VendorId).FirstOrDefaultAsync(cancellationToken)) ?? "vnd-02";
        }

        var inventoryId = Normalize(request.InventoryId);
        if (inventoryId is not null &&
            !await _context.InventoryItems.AnyAsync(
                item => item.InventoryId == inventoryId,
                cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                "Mặt hàng tồn kho không tồn tại");
        }

        var storeId = Normalize(request.StoreId);
        if (storeId is not null &&
            !await _context.RetailStores.AnyAsync(
                store => store.StoreId == storeId,
                cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                "Cửa hàng không tồn tại");
        }

        var equipment = new Equipment
        {
            EquipmentId = equipmentId,
            InventoryId = inventoryId,
            SerialNumber = serialNumber,
            MacAddress = macAddress,
            DeviceModel = deviceModel,
            DeviceType = deviceType,
            VendorId = vendorId,
            StoreId = storeId,
            Status = status,
            FirmwareVersion = Normalize(request.FirmwareVersion),
            CreatedAt = DateTime.Now
        };

        _context.Equipment.Add(equipment);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<EquipmentMutationResponse>.Success(
            new EquipmentMutationResponse(
                "Tạo thiết bị thành công",
                equipment.EquipmentId,
                equipment.Status));
    }

    public async Task<ServiceResult<EquipmentMutationResponse>> UpdateEquipmentAsync(
        string id,
        UpdateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var equipment = await _context.Equipment
            .Include(item => item.CustomerConnection)
            .FirstOrDefaultAsync(item => item.EquipmentId == id, cancellationToken);

        if (equipment is null)
        {
            return ServiceResult<EquipmentMutationResponse>.NotFound(
                "Không tìm thấy thiết bị");
        }

        var serialNumber = Normalize(request.SerialNumber);
        if (serialNumber is not null &&
            await _context.Equipment.AnyAsync(
                item => item.EquipmentId != id && item.SerialNumber == serialNumber,
                cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Conflict(
                "Serial number đã tồn tại");
        }


        var macAddress = Normalize(request.MacAddress);
        if (macAddress is not null &&
            await _context.Equipment.AnyAsync(
                item => item.EquipmentId != id && item.MacAddress == macAddress,
                cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Conflict(
                "Địa chỉ MAC đã tồn tại");
        }

        var storeId = Normalize(request.StoreId);
        if (storeId is not null &&
            !await _context.RetailStores.AnyAsync(
                store => store.StoreId == storeId,
                cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                "Cửa hàng không tồn tại");
        }

        var technicianId = Normalize(request.AssignedTechnicianId);
        if (technicianId is not null &&
            !await _context.Employees.AnyAsync(
                employee => employee.EmployeeId == technicianId && employee.Status == EmployeeStatuses.Active,
                cancellationToken))
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                "Kỹ thuật viên không tồn tại hoặc đã ngừng hoạt động");
        }

        var assignedAccountId = Normalize(request.AssignedAccountId);
        if (assignedAccountId is not null && assignedAccountId != equipment.AssignedAccountId)
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                "Hãy dùng API cấp kết nối để gán thiết bị cho tài khoản");
        }

        var status = request.Status is null
            ? null
            : NormalizeStatus(request.Status);
        if (request.Status is not null && status is null)
        {
            return ServiceResult<EquipmentMutationResponse>.Validation(
                $"Trạng thái không hợp lệ. Các giá trị cho phép: {string.Join(", ", EquipmentStatuses.All)}");
        }

        if (equipment.CustomerConnection is not null &&
            status is not null &&
            status != EquipmentStatuses.InService)
        {
            return ServiceResult<EquipmentMutationResponse>.Conflict(
                "Thiết bị đang phục vụ một kết nối, không thể đổi sang trạng thái này");
        }

        equipment.SerialNumber = serialNumber ?? equipment.SerialNumber;
        equipment.MacAddress = macAddress ?? equipment.MacAddress;
        equipment.DeviceModel = Normalize(request.DeviceModel) ?? equipment.DeviceModel;
        equipment.DeviceType = Normalize(request.DeviceType) ?? equipment.DeviceType;
        equipment.StoreId = storeId ?? equipment.StoreId;
        equipment.Status = status ?? equipment.Status;
        equipment.FirmwareVersion = Normalize(request.FirmwareVersion) ?? equipment.FirmwareVersion;
        equipment.AssignedTechnicianId = technicianId ?? equipment.AssignedTechnicianId;
        equipment.InstalledDate = request.InstalledDate ?? equipment.InstalledDate;
        equipment.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<EquipmentMutationResponse>.Success(
            new EquipmentMutationResponse(
                "Cập nhật thiết bị thành công",
                equipment.EquipmentId,
                equipment.Status));
    }

    private static IQueryable<EquipmentResponse> Project(IQueryable<Equipment> query)
    {
        return query.Select(equipment => new EquipmentResponse(
            equipment.EquipmentId,
            equipment.InventoryId,
            equipment.SerialNumber,
            equipment.MacAddress,
            equipment.DeviceModel,
            equipment.DeviceType,
            equipment.VendorId,
            equipment.Vendor.CompanyName,
            equipment.StoreId,
            equipment.Store == null ? null : equipment.Store.Name,
            equipment.Status,
            equipment.FirmwareVersion,
            equipment.AssignedAccountId,
            equipment.AssignedTechnicianId,
            equipment.AssignedTechnician == null
                ? null
                : equipment.AssignedTechnician.FullName,
            equipment.InstalledDate,
            equipment.CreatedAt,
            equipment.UpdatedAt));
    }

    /// <summary>
    /// Xoá một thiết bị khỏi kho. Chặn xoá nếu thiết bị đang lắp cho khách
    /// (đã gán AccountID hoặc đang ở trạng thái "In Service") để không mất dấu vết lắp đặt.
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteEquipmentAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var equipment = await _context.Equipment
            .Include(item => item.CustomerConnection)
            .FirstOrDefaultAsync(item => item.EquipmentId == id, cancellationToken);

        if (equipment is null)
        {
            return ServiceResult<bool>.NotFound("Không tìm thấy thiết bị");
        }

        if (equipment.CustomerConnection is not null ||
            !string.IsNullOrWhiteSpace(equipment.AssignedAccountId))
        {
            return ServiceResult<bool>.Conflict(
                "Thiết bị đang được cấp cho một kết nối của khách hàng nên không thể xoá. " +
                "Hãy thu hồi thiết bị trước.");
        }

        if (string.Equals(equipment.Status, EquipmentStatuses.InService, StringComparison.OrdinalIgnoreCase))
        {
            return ServiceResult<bool>.Conflict(
                "Thiết bị đang ở trạng thái In Service nên không thể xoá.");
        }

        _context.Equipment.Remove(equipment);
        await _context.SaveChangesAsync(cancellationToken);

        return ServiceResult<bool>.Success(true);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeStatus(string? value) =>
        EquipmentStatuses.All.FirstOrDefault(status =>
            string.Equals(status, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}
