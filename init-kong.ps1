Write-Host "🛠 Čakám na Kong, aby sa spustil..."
Start-Sleep -Seconds 10

Write-Host "🔄 Načítavam konfiguráciu z kong.yml do databázy..."
docker exec kong kong reload
docker exec kong kong config db_import /etc/kong/kong.yml

Write-Host "✅ Kong bol úspešne nakonfigurovaný!"
