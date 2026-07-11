using PayrollEngine.Core;
using PayrollEngine.Core.Models;

// ==================================================
// ربط قاعدة البيانات السحابية (Supabase)
// ⚠️ ملاحظة: هذا الرابط يحتوي على كلمة المرور. للتجربة فقط.
// ==================================================
var connectionString = "postgresql://postgres:P@ssw0rd2026sana@db.lgxpiezutknqrkiubczg.supabase.co:5432/postgres";

// تحميل الثوابت (نسب الضرائب والتأمين) من قاعدة البيانات الجديدة
var constants = await PostgresConstantsLoader.LoadAsync(connectionString);
var settings = new PayrollSettings(); // كل الإعدادات معطّلة (مثل الكود الأصلي)

var calculator = new PayrollCalculator(constants, settings);

// ----- بيانات الموظف التجريبي (قيم ثابتة للاختبار) -----
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

var health = new HealthLeaveInput
{
    UnpaidLeaveDaysThisMonth = 0,
    PaidSickLeaveDaysThisMonth = 2,
    PaidSickLeaveDaysThisYear = 10,
};

var loans = new List<decimal> { 3000, 1500 };
var punishments = new List<decimal> { };

// ----- تنفيذ الحساب -----
var result = calculator.Calculate(employee, health, loans, punishments);

// ----- طباعة النتيجة (بنفس التنسيق الجميل) -----
Console.WriteLine("=========== نتيجة الحساب التجريبية ===========");
Console.WriteLine($"الموظف: {result.FullName} (ID={result.EmployeeId})");
Console.WriteLine($"الراتب الأساسي (SALCUT):          {result.BaseSalary,12:N0}");
Console.WriteLine($"البدل العائلي (FAMILY):            {result.FamilyAllowance,12:N0}");
Console.WriteLine($"إجمالي الإضافات (SUM_ADDS):        {result.SumAdds,12:N0}");
Console.WriteLine("-----------------------------------------------");
Console.WriteLine($"التأمين (ASSUR):                   {result.Insurance,12:N0}");
Console.WriteLine($"الوعاء الضريبي (SALCUT1):          {result.TaxableBase,12:N0}");
Console.WriteLine($"ضريبة الدخل (TAXED):               {result.IncomeTax,12:N0}");
Console.WriteLine($"استقطاع مرضي (HEALTH):             {result.SickLeaveDeduction,12:N0}");
Console.WriteLine($"استقطاع إجازة بدون أجر:            {result.UnpaidLeaveDeduction,12:N0}");
Console.WriteLine($"أقساط القروض (DENT):               {result.LoanDeductions,12:N0}");
Console.WriteLine($"العقوبات (XGIVE1):                 {result.PunishmentDeduction,12:N0}");
Console.WriteLine($"إجمالي الاستقطاعات (SUM_CUTS):     {result.SumCuts,12:N0}");
Console.WriteLine("-----------------------------------------------");
Console.WriteLine($"الصافي الأساسي (PURE):             {result.PureBase,12:N0}");
Console.WriteLine($"صافي بدل التخصص:                   {result.PureSpecialtyAllowance,12:N0}");
Console.WriteLine($"صافي بدل نوع الوظيفة:              {result.PureJobTypeAllowance,12:N0}");
Console.WriteLine($"صافي بدل المسؤولية:                {result.PureResponsibilityAllowance,12:N0}");
Console.WriteLine($"صافي بدل الصحفيين:                 {result.PureJournalistAllowance,12:N0}");
Console.WriteLine("=================================================");
Console.WriteLine($"✅ الصافي النهائي (PURRR):         {result.NetTotal,12:N0}");
Console.WriteLine("=================================================");

if (result.Notes.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("ملاحظات:");
    foreach (var note in result.Notes) Console.WriteLine($" - {note}");
}