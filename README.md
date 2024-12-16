# Arena Service

Arena Service is a standalone service that manages the PvP system for Nine Chronicles. It separates score calculation and matching systems from the blockchain to provide a more flexible and scalable architecture.

## System Architecture

The service operates between the game client and the blockchain, managing all arena-related data and game logic while using the blockchain only for battle verification and recording.

```mermaid
sequenceDiagram
    actor User
    participant Client
    participant ArenaService
    participant Blockchain
    participant DB

    Note over User,DB: Arena Entry & Opponent List
    User->>Client: Enter Arena
    Client->>ArenaService: Request Current Rank/Score
    ArenaService->>DB: Query User Status
    DB-->>ArenaService: Current Rank/Score
    ArenaService->>ArenaService: Generate Opponent List<br>(Based on Ranking Groups)
    ArenaService-->>Client: Return Rank/Score/Opponents
    Client-->>User: Display Arena UI

    Note over User,DB: Battle Process
    User->>Client: Start Battle
    Client->>ArenaService: Validate Battle Request
    ArenaService->>ArenaService: Verify Conditions<br>(Tickets/Valid Opponent)
    ArenaService-->>Client: Validation Result
    Client->>Blockchain: Arena Battle TX
    Blockchain-->>ArenaService: Battle Result Event
    ArenaService->>ArenaService: Calculate Score
    ArenaService->>DB: Save Battle Result/Score
    ArenaService-->>Client: Updated Information
    Client-->>User: Display Result
```

## Getting Started

1. Install dependencies:
```bash
dotnet restore
```

2. Update database:
```bash
dotnet ef database update
```

3. Run the service:
```bash
dotnet run
```

## API Documentation

Detailed API documentation is available through Swagger at `/swagger` when running in development mode.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
