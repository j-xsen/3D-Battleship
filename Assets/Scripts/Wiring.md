```mermaid
---
title: Runtime wiring
---
flowchart TD
subgraph Board
TurnManager
SpaceBuilder
CameraRotator
end

    subgraph Network
        SessionManager
    end

    subgraph Hover
        HoverMouseControls
        HoverActions
    end

    subgraph Ships
        ShipManager
        ShipPlacementUI
        ShipTypeManager
    end

    HoverMouseControls -->|fires events| HoverActions
    HoverActions -->|updates position| SpaceBuilder
    HoverActions -->|Clicked / ShipClicked| ShipManager
    SpaceBuilder -->|OnCursorMoved| ShipManager
    TurnManager -->|manages| ShipManager
    TurnManager -->|manages| SpaceBuilder
    CameraRotator -->|reads origin| SpaceBuilder
    ShipManager -->|notifies on place| SessionManager
    ShipManager -->|uses| ShipTypeManager
    ShipPlacementUI -->|reads counts| ShipManager
    ShipPlacementUI -->|reads rations| ShipTypeManager
```
