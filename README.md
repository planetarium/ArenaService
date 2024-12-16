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

    Note over User,DB: Arena Registration
    User->>Client: Request Arena Registration (Previous season participants are auto-registered)
    Client->>ArenaService: Send Registration Request
    ArenaService->>DB: Check Registration Status
    DB-->>ArenaService: Return Registration Status
    ArenaService->>DB: Save New Registration (if needed)
    ArenaService-->>Client: Respond with Registration Complete
    Client-->>User: Display Registration Result

    Note over User,DB: Update Opponent List
    User->>Client: Request List Update
    Client->>ArenaService: Send Update Request
    ArenaService->>ArenaService: Verify Costs
    ArenaService->>DB: Fetch Current Ranking
    DB-->>ArenaService: Return Ranking Info
    ArenaService->>ArenaService: Generate New List
    ArenaService->>DB: Save New List
    ArenaService-->>Client: Provide Updated Opponent List
    Client-->>User: Display Updated UI

    Note over User,DB: Conduct Battle
    User->>Client: Start Battle
    Client->>ArenaService: Validate Battle Request
    ArenaService->>ArenaService: Check Conditions (Tickets/Opponent Availability)
    ArenaService->>ArenaService: Sign Battle Token
    ArenaService-->>Client: Provide Validation Result and Token
    Client->>Blockchain: Send Arena Battle TX (with Token)
    Blockchain-->>ArenaService: Battle Result Event (including TX ID)
    ArenaService->>ArenaService: Poll TX and Verify Result
    ArenaService->>ArenaService: Calculate Scores and Generate Result
    ArenaService->>DB: Save Battle Result/Scores
    ArenaService-->>Client: Provide Updated Information
    Client-->>User: Display Battle Result
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
