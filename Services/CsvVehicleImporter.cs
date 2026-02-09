using Cartrade.Models;
using Cartrade.Models.Enums;

namespace Cartrade.Services;

public sealed class CsvVehicleImporter
{
    private static readonly Dictionary<string, string[]> HeaderAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ExternalReference"] = ["externalreference", "externalid", "reference", "ref"],
        ["RegistrationNumber"] = ["registrationnumber", "regnr", "registration", "plate"],
        ["Vin"] = ["vin", "chassisnumber", "chassis"],
        ["Make"] = ["make", "brand", "marke"],
        ["Model"] = ["model"],
        ["ModelYear"] = ["modelyear", "year", "arsmodell"],
        ["OdometerKm"] = ["odometerkm", "mileage", "km", "korda_km"],
        ["Color"] = ["color", "farg"],
        ["FuelType"] = ["fueltype", "fuel", "drivmedel"],
        ["PartnerName"] = ["partnername", "partner", "supplier", "leverantor"]
    };

    public async Task<(List<Vehicle> Vehicles, List<string> Warnings)> ParseAsync(
        Stream stream,
        string? createdByUserId,
        CancellationToken cancellationToken = default)
    {
        var vehicles = new List<Vehicle>();
        var warnings = new List<string>();
        var seenRegistrationNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            warnings.Add("Filen saknar rubrikrad.");
            return (vehicles, warnings);
        }

        var headers = headerLine.Split(';').Select(Normalize).ToArray();

        var lineNumber = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var cells = line.Split(';');
            var reg = ReadValue(cells, headers, "RegistrationNumber").ToUpperInvariant();
            var make = ReadValue(cells, headers, "Make");
            var model = ReadValue(cells, headers, "Model");

            if (string.IsNullOrWhiteSpace(reg) || string.IsNullOrWhiteSpace(make) || string.IsNullOrWhiteSpace(model))
            {
                warnings.Add($"Rad {lineNumber} hoppades över: registreringsnummer/märke/modell saknas.");
                continue;
            }

            if (!seenRegistrationNumbers.Add(reg))
            {
                warnings.Add($"Rad {lineNumber} hoppades över: dubbelt registreringsnummer i filen ({reg}).");
                continue;
            }

            _ = int.TryParse(ReadValue(cells, headers, "ModelYear"), out var modelYear);
            _ = int.TryParse(ReadValue(cells, headers, "OdometerKm"), out var odometerKm);

            vehicles.Add(new Vehicle
            {
                ExternalReference = ReadValue(cells, headers, "ExternalReference"),
                RegistrationNumber = reg,
                Vin = ReadValue(cells, headers, "Vin"),
                Make = make,
                Model = model,
                ModelYear = modelYear == 0 ? null : modelYear,
                OdometerKm = odometerKm,
                Color = ReadValue(cells, headers, "Color"),
                FuelType = ReadValue(cells, headers, "FuelType"),
                PartnerName = ReadValue(cells, headers, "PartnerName"),
                Source = VehicleSource.FileImport,
                Status = VehicleStatus.Registered,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return (vehicles, warnings);
    }

    private static string ReadValue(string[] cells, string[] headers, string logicalName)
    {
        var aliases = HeaderAliases[logicalName];
        var index = Array.FindIndex(headers, h => aliases.Contains(h, StringComparer.OrdinalIgnoreCase));
        if (index < 0 || index >= cells.Length)
        {
            return string.Empty;
        }

        return cells[index].Trim();
    }

    private static string Normalize(string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("-", string.Empty)
            .Replace("_", string.Empty);
    }
}
