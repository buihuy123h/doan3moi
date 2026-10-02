using doanki3API.Data;
using Microsoft.EntityFrameworkCore;

namespace doanki3API.Services;

/// <summary>
/// Generates the canonical account ID (16 characters):
/// connection-type letter + three-digit city code + twelve-digit global serial.
/// Call this service inside a serializable transaction when the ID is persisted.
/// </summary>
public sealed class AccountIdGenerator
{
    private readonly MyAppDbContext _context;

    public AccountIdGenerator(MyAppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(
        string connectionType,
        string cityCode,
        CancellationToken cancellationToken)
    {
        var normalizedCityCode = cityCode.Trim();
        if (normalizedCityCode.Length != 3 || !normalizedCityCode.All(char.IsDigit))
            throw new InvalidOperationException("CityCode must contain exactly three digits.");

        var prefix = connectionType.Trim().ToUpperInvariant() switch
        {
            "BROADBAND" => "B",
            "DIAL-UP" or "DIALUP" => "D",
            "LANDLINE" or "TELEPHONE" => "T",
            _ => throw new InvalidOperationException(
                $"Unsupported connection type '{connectionType}'.")
        };

        var connectionIds = await _context.CustomerConnections
            .AsNoTracking()
            .Select(connection => connection.AccountId)
            .ToListAsync(cancellationToken);
        var reservedIds = await _context.Orders
            .AsNoTracking()
            .Where(order => order.AssignedAccountId != null)
            .Select(order => order.AssignedAccountId!)
            .ToListAsync(cancellationToken);

        var nextSerial = connectionIds
            .Concat(reservedIds)
            .Select(ParseSerial)
            .DefaultIfEmpty(0)
            .Max() + 1;

        if (nextSerial > 999_999_999_999)
            throw new InvalidOperationException("Account ID serial range is exhausted.");

        return $"{prefix}{normalizedCityCode}{nextSerial:D12}";
    }

    private static long ParseSerial(string accountId)
    {
        var significantCharacters = accountId
            .Where(char.IsLetterOrDigit)
            .ToArray();
        if (significantCharacters.Length != 16)
            return 0;

        return long.TryParse(significantCharacters.AsSpan(4, 12), out var serial)
            ? serial
            : 0;
    }
}
