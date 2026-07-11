using Npgsql;
using PayrollEngine.Core.Models;

namespace PayrollEngine.Core;

public static class PostgresConstantsLoader
{
    public static async Task<PayrollConstants> LoadAsync(string connectionString)
    {
        var constants = new PayrollConstants();
        var taxBrackets = new List<TaxBracket>();

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        // 1. تحميل الشرائح الضريبية (جدول tax_brackets)
        await using var cmdBrackets = new NpgsqlCommand(
            "SELECT bracket_number, upper_limit, rate_percent FROM tax_brackets ORDER BY bracket_number;", 
            conn);
        await using var reader = await cmdBrackets.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            taxBrackets.Add(new TaxBracket
            {
                Number = reader.GetInt32(0),
                UpperLimit = reader.GetDecimal(1),
                RatePercent = reader.GetDecimal(2)
            });
        }
        constants.TaxBrackets = taxBrackets;
        await reader.CloseAsync();

        // 2. تحميل بقية الثوابت (جدول app_constants)
        await using var cmdConsts = new NpgsqlCommand(
            "SELECT key_name, value_numeric FROM app_constants;", 
            conn);
        await using var constReader = await cmdConsts.ExecuteReaderAsync();
        while (await constReader.ReadAsync())
        {
            var key = constReader.GetString(0);
            var val = constReader.GetDecimal(1);

            // ربط المفاتيح بالخصائص الفعلية
            if (key == "WIFE_ALLOWANCE") constants.WifeAllowanceUnit = val;
            else if (key == "CHILD_ALLOWANCE") constants.RegularChildAllowanceUnit = val;
            else if (key == "INSURANCE_RATE_LAW_1") constants.InsuranceRateByLawId[1] = val;
            else if (key == "MAX_SICK_DAYS") constants.MaxPaidSickDaysPerYear = (int)val;
            else if (key == "SICK_PAY_RATE") constants.SickDayPayRate = val;
        }

        await conn.CloseAsync();
        return constants;
    }
}