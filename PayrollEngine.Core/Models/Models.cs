namespace PayrollEngine.Core.Models;

public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public int Kind { get; set; }
    public int LawId { get; set; }
    public bool IsJournalist { get; set; }
    public bool HasWife { get; set; }
    public int ChildrenCount { get; set; }
    public bool Son1SpecialCase { get; set; }
    public bool Son2SpecialCase { get; set; }
    public bool Son3SpecialCase { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal InsuranceSalaryBase { get; set; }
    public decimal TopSalaryCeiling { get; set; }
    public decimal TaxExemptAmount { get; set; }
    public decimal TaxFreeThreshold { get; set; }
    public decimal ChildTaxExemption { get; set; }
    public decimal SpecialtyAllowanceValue { get; set; }
    public decimal JobTypeAllowanceValue { get; set; }
    public decimal ResponsibilityAllowanceValue { get; set; }
    public decimal JournalistAllowanceValue { get; set; }
    public decimal DrawAllowance { get; set; }
    public decimal HeatAllowance { get; set; }
    public decimal EstdrakValue { get; set; }
    public decimal WorkHouseHours { get; set; }
    public decimal WorkerAllowance { get; set; }
    public decimal EngineerCutsPercent { get; set; }
}

public class PayrollResult
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = "";
    public decimal BaseSalary { get; set; }
    public decimal FamilyAllowance { get; set; }
    public decimal SumAdds { get; set; }
    public decimal Insurance { get; set; }
    public decimal TaxableBase { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal SickLeaveDeduction { get; set; }
    public decimal UnpaidLeaveDeduction { get; set; }
    public decimal LoanDeductions { get; set; }
    public decimal PunishmentDeduction { get; set; }
    public decimal SumCuts { get; set; }
    public decimal PureBase { get; set; }
    public decimal PureSpecialtyAllowance { get; set; }
    public decimal PureJobTypeAllowance { get; set; }
    public decimal PureResponsibilityAllowance { get; set; }
    public decimal PureJournalistAllowance { get; set; }
    public decimal NetTotal { get; set; }
    public List<string> Notes { get; } = new();
}

public class TaxBracket
{
    public int Number { get; set; }
    public decimal UpperLimit { get; set; }
    public decimal RatePercent { get; set; }
}

public class PayrollConstants
{
    public List<TaxBracket> TaxBrackets { get; set; } = new();
    public decimal WifeAllowanceUnit { get; set; } = 1;
    public decimal RegularChildAllowanceUnit { get; set; } = 1;
    public decimal Son1SpecialBonus { get; set; } = 0;
    public decimal Son2SpecialBonus { get; set; } = 0;
    public decimal Son3SpecialBonus { get; set; } = 0;
    public Dictionary<int, decimal> InsuranceRateByLawId { get; set; } = new();
    public int MaxPaidSickDaysPerYear { get; set; } = 30;
    public decimal SickDayPayRate { get; set; } = 0.2m;
}

public class PayrollSettings
{
    public bool ApplyChildTaxExemption { get; set; } = false;
    public bool ApplySickDaysProration { get; set; } = false;
    public bool ApplyEngineerCuts { get; set; } = false;
    public bool ApplyInlineJournalistDeductions { get; set; } = false;
    public bool PunishmentAsPercentageOfSalary { get; set; } = false;
}

public class HealthLeaveInput
{
    public decimal UnpaidLeaveDaysThisMonth { get; set; }
    public decimal PaidSickLeaveDaysThisMonth { get; set; }
    public decimal PaidSickLeaveDaysThisYear { get; set; }
}