FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# نسخ ملفات المشروع
COPY PayrollEngine.Core/*.csproj ./PayrollEngine.Core/
COPY PayrollEngine.Executor/*.csproj ./PayrollEngine.Executor/
RUN dotnet restore "PayrollEngine.Executor/PayrollEngine.Executor.csproj"

# نسخ باقي الملفات وبناء التطبيق
COPY . .
RUN dotnet publish "PayrollEngine.Executor/PayrollEngine.Executor.csproj" -c Release -o /app

# تشغيل التطبيق
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PayrollEngine.Executor.dll"]
