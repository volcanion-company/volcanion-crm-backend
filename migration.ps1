# lấy thư mục hiện tại:
$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

# di chuyển đến thư mục chứa project api:
Write-Host "Changing directory to $currentDir/src/CrmSaas.Api" -ForegroundColor Yellow
Set-Location "$currentDir/src/CrmSaas.Api"

# chạy lệnh cập nhật database:
Write-Host "Migration database with MasterDbContext" -ForegroundColor Yellow
dotnet ef database update --context MasterDbContext
Write-Host "Migration database with TenantDbContext" -ForegroundColor Yellow
dotnet ef database update --context TenantDbContext

# quay trở lại thư mục ban đầu:
Write-Host "Changing directory back to $currentDir" -ForegroundColor Yellow
Set-Location $currentDir

# chạy lệnh xoá fk bằng file scripts - sử dụng Windows Authentication (-E):
Write-Host "Dropping foreign key constraints from CrmSaas_Master_Dev database" -ForegroundColor Yellow
sqlcmd -S "localhost" -d "CrmSaas_Master_Dev" -E -i "$currentDir\scripts\drop_fk_constraints.sql"

Write-Host "Migration completed successfully!" -ForegroundColor Green