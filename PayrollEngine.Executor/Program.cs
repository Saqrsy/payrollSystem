using PayrollEngine.Core;
using PayrollEngine.Core.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async () =>
{
    var connectionString = "postgresql://postgres:P@ssw0rd2026sana@db.lgxpiezutknqrkiubczg.supabase.co:5432/postgres";
    
    var constants = await PostgresConstantsLoader.LoadAsync(connectionString);
    var settings = new PayrollSettings();
    var calculator = new PayrollCalculator(constants, settings);

    var employee = new Employee
    {
        Id = 1,
        FullName = "أحمد السوري",
        Kind = 1,
        LawId = 1,
        IsJournalist = true,
        HasWife = true,
        ChildrenCount = 2,
        BaseSalary = 120000,
        InsuranceSalaryBase = 120000,
        TaxFreeThreshold = 30000,
        TaxExemptAmount = 0,
        SpecialtyAllowanceValue = 15000,
        JobTypeAllowanceValue = 8000,
        ResponsibilityAllowanceValue = 5000,
        JournalistAllowanceValue = 10000,
    };

    // ✅ الحل: استخدم الاسم الكامل هنا
    var health = new PayrollEngine.Core.Models.HealthLeaveInput
    {
        UnpaidLeaveDaysThisMonth = 0,
        PaidSickLeaveDaysThisMonth = 2,
        PaidSickLeaveDaysThisYear = 10,
    };

    var loans = new List<decimal> { 3000, 1500 };
    var punishments = new List<decimal> { };

    var result = calculator.Calculate(employee, health, loans, punishments);

    return Results.Json(new
    {
        employee = result.FullName,
        netSalary = result.NetTotal,
        details = new
        {
            result.Insurance,
            result.IncomeTax,
            result.SumAdds,
            result.SumCuts,
            result.PureBase,
            result.FamilyAllowance,
            result.SickLeaveDeduction,
            result.LoanDeductions
        }
    });
});

app.Run();