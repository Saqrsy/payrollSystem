using PayrollEngine.Core.Models;

namespace PayrollEngine.Core;

/// <summary>
/// إعادة بناء دقيقة لمنطق AC_S_Calcsal.fmb (المحرك المعتمد)، بنفس ترتيب الخطوات
/// وبنفس أسماء المتغيرات الأصلية (موضوعة بالتعليقات) لتسهيل المراجعة والتدقيق سطراً بسطر.
///
/// كل بند فيه ⚠️ بالتعليقات هو نقطة كانت "كود ميت" أو مثيرة للجدل بالنظام الأصلي،
/// وسلوكها هون قابل للتحكم عبر PayrollSettings تماماً كما هي فعلياً بالنظام القديم (معطّلة).
/// </summary>
public class PayrollCalculator
{
    private readonly PayrollConstants _constants;
    private readonly PayrollSettings _settings;

    public PayrollCalculator(PayrollConstants constants, PayrollSettings settings)
    {
        _constants = constants;
        _settings = settings;
    }

    public PayrollResult Calculate(Employee emp, HealthLeaveInput health, List<decimal> monthlyLoanInstallments, List<decimal> punishmentAmounts)
    {
        var result = new PayrollResult { EmployeeId = emp.Id, FullName = emp.FullName, BaseSalary = emp.BaseSalary };

        // ===================== 1) التأمينات الاجتماعية (ASSUR) =====================
        var insuranceRate = _constants.InsuranceRateByLawId.GetValueOrDefault(emp.LawId, 0m);
        var assur = Math.Round(insuranceRate * emp.InsuranceSalaryBase / 100m, 0);
        result.Insurance = assur;

        // ===================== 2) البدل العائلي (FAMILY) =====================
        decimal family = emp.HasWife ? Math.Ceiling(_constants.WifeAllowanceUnit) : 0m;
        // SUN1/SUN2/SUN3: حالات أبناء خاصة -- تُضاف كقيمة ثابتة لكل حالة
        if (emp.Son1SpecialCase) family += _constants.Son1SpecialBonus;
        if (emp.Son2SpecialCase) family += _constants.Son2SpecialBonus;
        if (emp.Son3SpecialCase) family += _constants.Son3SpecialBonus;
        // بدل الأولاد العاديين
        family += Math.Ceiling(emp.ChildrenCount * _constants.RegularChildAllowanceUnit);
        result.FamilyAllowance = family;

        // ===================== 3) الاستقطاع المرضي =====================
        // 3.a: إجازة مرضية "بدون أجر" (NON_SAL) -- تُخصم كامل الراتب اليومي
        decimal unpaidLeaveDeduction = 0m;
        if (health.UnpaidLeaveDaysThisMonth > 0)
        {
            unpaidLeaveDeduction = Math.Ceiling((emp.BaseSalary + family) * health.UnpaidLeaveDaysThisMonth / 30m);
        }
        result.UnpaidLeaveDeduction = unpaidLeaveDeduction;

        // ⚠️ PAR1 بالكود الأصلي دايماً ينتهي =1 (كود ميت) -- هون قابل للتفعيل عبر الإعداد
        decimal par1 = 1m;
        if (_settings.ApplySickDaysProration && health.UnpaidLeaveDaysThisMonth > 0)
        {
            par1 = (30m - health.UnpaidLeaveDaysThisMonth) / 30m;
        }

        // 3.b: إجازة مرضية عادية -- 20% من الراتب اليومي، بسقف 30 يوم بالسنة
        decimal sickDeduction = 0m;
        decimal monthHealthDays = health.PaidSickLeaveDaysThisMonth;
        if (health.PaidSickLeaveDaysThisYear <= _constants.MaxPaidSickDaysPerYear)
        {
            sickDeduction = Math.Round((emp.BaseSalary / 30m) * _constants.SickDayPayRate * monthHealthDays, 0);
        }
        else if (health.PaidSickLeaveDaysThisYear - monthHealthDays < _constants.MaxPaidSickDaysPerYear)
        {
            var remaining = _constants.MaxPaidSickDaysPerYear - (health.PaidSickLeaveDaysThisYear - monthHealthDays);
            sickDeduction = Math.Round((emp.BaseSalary / 30m) * _constants.SickDayPayRate * remaining, 0);
        }
        // ⚠️ غير موثّق بالكود الأصلي: شو بيصير للأيام يلي تتجاوز الـ30 -- بانتظار تأكيد خبير الرواتب
        result.SickLeaveDeduction = sickDeduction;

        // ===================== 4) الوعاء الضريبي (SUP_TAX) =====================
        var salcut1 = emp.BaseSalary - assur;
        salcut1 = salcut1 > emp.TaxFreeThreshold ? salcut1 - emp.TaxFreeThreshold : 0m;
        result.TaxableBase = salcut1;

        // ===================== 5) الضريبة التصاعدية (TAXED_ON_SAL) =====================
        // ⚠️ مؤكد من الكود الفعلي: الضريبة تُحسب على (SALCUT1 + FAMILY) قبل خصم أي استقطاع مرضي
        var taxBase = salcut1 + family;
        var tax = CalculateProgressiveTax(taxBase, emp.TaxExemptAmount);
        // ⚠️ CHILD_TAX: موجود بالكود كاستدعاء لكن نتيجته مُهملة دائماً -- معطّل هون تطابقاً لذلك
        if (_settings.ApplyChildTaxExemption)
        {
            tax = CalculateProgressiveTax(taxBase, emp.TaxExemptAmount + emp.ChildTaxExemption);
        }
        tax = Math.Ceiling(tax * par1);
        result.IncomeTax = tax;

        // ===================== 6) القروض والسلف (DENT) =====================
        var loanTotal = Math.Ceiling(monthlyLoanInstallments.Sum());
        result.LoanDeductions = loanTotal;

        // ===================== 7) العقوبات (PUNSH) =====================
        // ⚠️ مؤكد: مبلغ ثابت من الكود الفعلي وليس نسبة (رغم وجود كود نسبة غير مُستخدم)
        decimal punishment;
        if (_settings.PunishmentAsPercentageOfSalary)
            punishment = Math.Ceiling(punishmentAmounts.Sum() * emp.BaseSalary / 100m);
        else
            punishment = punishmentAmounts.Sum();
        result.PunishmentDeduction = punishment;

        // ===================== 8) قطع الهندسة =====================
        decimal engCuts = 0m;
        if (_settings.ApplyEngineerCuts)
            engCuts = Math.Round(emp.BaseSalary * emp.EngineerCutsPercent / 100m, 0);

        // ===================== 9) بدلات الصحفيين "الداخلية" (مُعطَّلة افتراضياً) =====================
        decimal inlineJournalistDeductions = 0m; // HLTJOUR+DEDJOUR+SHKJOUR+ASURRJOUR+NSABJOUR
        // (تبقى صفر إلا إذا فُعِّلت صراحة -- بانتظار تأكيد أنها فعلاً منقولة لشاشة AC_S_ADD_JOUR)

        // ===================== 10) إجمالي الاستقطاعات (SUM_CUTS) =====================
        result.SumCuts = assur + tax + sickDeduction + unpaidLeaveDeduction + loanTotal
                        + punishment + engCuts + inlineJournalistDeductions;

        // ===================== 11) إجمالي الإضافات (SUM_ADDS) =====================
        result.SumAdds = Math.Ceiling(emp.BaseSalary) + family + emp.HeatAllowance + emp.EstdrakValue + emp.DrawAllowance
                        + emp.WorkHouseHours + Math.Ceiling(emp.WorkerAllowance);

        // ===================== 12) الصافي الأساسي (PURE) =====================
        result.PureBase = Math.Round(result.SumAdds - result.SumCuts, 0);

        // ===================== 13) صافي البدلات الإضافية (منطق SPEC الهامشي) =====================
        result.PureSpecialtyAllowance = CalculateMarginalAllowanceNet(emp.BaseSalary, emp.SpecialtyAllowanceValue);
        result.PureJobTypeAllowance = CalculateMarginalAllowanceNet(emp.BaseSalary, emp.JobTypeAllowanceValue);
        result.PureResponsibilityAllowance = CalculateMarginalAllowanceNet(emp.BaseSalary, emp.ResponsibilityAllowanceValue);
        result.PureJournalistAllowance = emp.IsJournalist
            ? CalculateMarginalAllowanceNet(emp.BaseSalary, emp.JournalistAllowanceValue)
            : 0m;

        // ===================== 14) ✅ الصافي النهائي الحقيقي (PURRR مؤكد) =====================
        result.NetTotal = result.PureBase
                         + result.PureSpecialtyAllowance
                         + result.PureJobTypeAllowance
                         + result.PureResponsibilityAllowance
                         + result.PureJournalistAllowance;

        if (!_settings.ApplyChildTaxExemption)
            result.Notes.Add("⚠️ إعفاء CHILD_TAX معطّل (يطابق سلوك الكود الأصلي الفعلي المُنفَّذ).");
        if (!_settings.ApplySickDaysProration)
            result.Notes.Add("⚠️ تناسب أيام المرض (PAR1) معطّل (يطابق سلوك الكود الأصلي الفعلي).");

        return result;
    }

    /// <summary>
    /// إعادة بناء دالة SPEC الأصلية: ضريبة البدل الإضافي حسب الشريحة الهامشية
    /// التي يقع فيها هذا البدل تحديداً فوق الراتب الأساسي (مع تقسيم دقيق إذا امتد بين شريحتين).
    /// </summary>
    private decimal CalculateMarginalAllowanceNet(decimal baseSalary, decimal allowanceValue)
    {
        if (allowanceValue <= 0) return 0m;

        var brackets = _constants.TaxBrackets.OrderBy(b => b.Number).ToList();
        decimal totalSalaryWithAllowance = baseSalary;
        decimal salaryBeforeAllowance = baseSalary - allowanceValue;

        decimal taxOnAllowance = 0m;
        decimal previousLimit = 0m;

        foreach (var bracket in brackets)
        {
            if (totalSalaryWithAllowance <= previousLimit) break;

            var upperOfThisBracket = Math.Min(totalSalaryWithAllowance, bracket.UpperLimit);
            var portionOfAllowanceInBracket = Math.Max(0m, upperOfThisBracket - Math.Max(previousLimit, salaryBeforeAllowance));

            if (portionOfAllowanceInBracket > 0)
            {
                taxOnAllowance += portionOfAllowanceInBracket * bracket.RatePercent / 100m;
            }

            previousLimit = bracket.UpperLimit;
            if (totalSalaryWithAllowance <= bracket.UpperLimit) break;
        }

        return Math.Round(allowanceValue - taxOnAllowance, 0);
    }

    /// <summary>إعادة بناء دالة TAXED_ON_SAL الأصلية -- ضريبة تصاعدية بـ5 شرائح (مؤكد، وليس 8).</summary>
    private decimal CalculateProgressiveTax(decimal taxableSalary, decimal exemptAmount)
    {
        var brackets = _constants.TaxBrackets.OrderBy(b => b.Number).ToList();
        decimal tax = 0m;
        decimal previousLimit = 0m;

        foreach (var bracket in brackets)
        {
            if (taxableSalary <= previousLimit) break;

            var upper = Math.Min(taxableSalary, bracket.UpperLimit);
            var portion = upper - previousLimit;

            if (bracket.Number == 1)
                portion = Math.Max(0m, portion - exemptAmount); // الإعفاء يُطبَّق على الشريحة الأولى فقط، تطابقاً للكود الأصلي

            tax += portion * bracket.RatePercent / 100m;
            previousLimit = bracket.UpperLimit;
        }

        return tax;
    }
}

public class HealthLeaveInput
{
    public decimal UnpaidLeaveDaysThisMonth { get; set; }
    public decimal PaidSickLeaveDaysThisMonth { get; set; }
    public decimal PaidSickLeaveDaysThisYear { get; set; }
}
