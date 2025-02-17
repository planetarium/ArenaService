docker compose -f docker-compose.test.yml run --build --rm test-runner

dotnet ef migrations add AddExceptionNames --project ArenaService.Shared --startup-project ArenaService
dotnet ef database update --project ArenaService.Shared --startup-project ArenaService
