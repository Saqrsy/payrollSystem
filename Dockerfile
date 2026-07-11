FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "PayrollEngine.Executor/PayrollEngine.Executor.csproj"
RUN dotnet publish "PayrollEngine.Executor/PayrollEngine.Executor.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PayrollEngine.Executor.dll"]
