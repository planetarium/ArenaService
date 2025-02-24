# Contributor Guide

ArenaService is an **ASP.NET-based backend application** that interacts with the **NineChronicles Chain (Headless)**.  
It leverages **Hangfire** for asynchronous task execution and **PostgreSQL** as its primary database.

## Project Structure

ArenaService is structured as follows:

- **ArenaService** → The main application handling Arena operations.
- **ArenaService.BackOffice** → A management interface for DB and Arena administration.
- **ArenaService.Shared** → A shared library containing database models and repositories, used by both `ArenaService` and `ArenaService.BackOffice`.
- **ArenaService.Tests** → Unit tests.
- **ArenaService.IntegrationTests** → Integration tests.

## Running Locally

### Prerequisites

To run ArenaService locally, **Redis** and **PostgreSQL** are required.  
For convenience, a `docker-compose.yml` file is provided with local **PostgreSQL** and **Redis** configurations.

First, install [Docker](https://www.docker.com/) if you haven't already.

### Start Dependencies

Run the following command to start PostgreSQL and Redis:

```sh
docker compose up -d
```

Expected output:
```
[+] Running 2/2
 ✔ Container arena_postgres  Running
 ✔ Container arena_redis     Running
```

### Configuration

Create a **local configuration file** for ArenaService.  
Navigate to the `ArenaService` directory and create a file named **`appsettings.Local.json`**.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=heimdall_arena;Username=local_test;Password=password"
  },
  "Redis": {
    "Host": "127.0.0.1",
    "Port": 6379
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  },
  "AllowedHosts": "*",
  "Headless": {
    "HeadlessEndpoint": "https://odin-rpc.nine-chronicles.com/graphql"
  },
  "OpsConfig": {
    "RecipientAddress": "{NineChronicles Agent Address}",
    "ArenaProviderName": "{Your Provider Name}",
    "JwtSecretKey": "{Your Secret Key}",
    "JwtPublicKey": "{Your Public Key}",
    "HangfireUsername": "admin",
    "HangfirePassword": "testtest"
  }
}
```

### Configuration Details:

- **`HeadlessEndpoint`** → Specify the blockchain node to connect to.
- **`RecipientAddress`** → Address for receiving NCG rewards.
- **`ArenaProviderName`** → Name of the provider recorded on-chain. If you want to become a new Arena operator, add your provider name to the [ArenaProvider Enum](https://github.com/planetarium/lib9c/blob/development/Lib9c/Action/Arena/ArenaProvider.cs).
  - If you're testing, you can use `"PLANETARIUM"`.
- **`JwtSecretKey` & `JwtPublicKey`** → Used for **Battle Token verification**.
  - Generate a private key:
    ```sh
    openssl genpkey -algorithm RSA -out private.pem -pkeyopt rsa_keygen_bits:2048
    ```
  - Convert it to **Base64**:
    ```sh
    base64 -i private.pem
    ```
  - Generate a public key from it:
    ```sh
    openssl rsa -pubout -in private.pem -out public.pem
    ```
  - Convert the public key to **Base64** as well.
- **`HangfireUsername` & `HangfirePassword`** → Credentials for accessing the Hangfire management dashboard.


### Database Migration

Before running the service, **migrate the database**:

```sh
ASPNETCORE_ENVIRONMENT=Local dotnet ef database update --project ArenaService.Shared --startup-project ArenaService
```

### Run ArenaBackOffice

백오피스를 실행해서 데이터를 설정해야함


## Running Tests

ArenaService includes **unit tests** and **integration tests**.  
Run the tests with:

```sh
dotnet test ArenaService.Tests
```

For integration tests:

```sh
dotnet test ArenaService.IntegrationTests
```

## Create Migration
```
dotnet ef migrations add {Name} --project ArenaService.Shared --startup-project ArenaService
```

## Need Help?

- **Join our Discord** for discussions: [Planetarium Dev Discord](https://planetarium.dev/discord)
- **Check out our Wiki**: [ArenaService Wiki](https://github.com/planetarium/ArenaService/wiki)
