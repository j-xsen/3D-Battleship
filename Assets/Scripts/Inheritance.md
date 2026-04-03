```mermaid
---
title: Inheritance
---
classDiagram
class Ship {
<<abstract>>
Base ship model
+HasValidPlacement(Vector3) bool
+Rotate(Transform)
+SetPosition(Vector3)
+GetAxes() AxisObject
}
class LineShip {
Straight line ship model
+HasValidPlacement(Vector3) bool
}
class ShipView {
<<abstract>>
Base ship MonoBehaviour
+HasValidPlacement(Vector3) bool
+Rotate()
+MoveShip(Vector3, Vector3)
+GetAxes() AxisObject
+SetMaterial(Material)
}
class LineShipView {
MonoBehaviour for line ship
+shipLength int
+shipHealth int
}
class AxisObject {
<<abstract>>
Represents a placement axis
+GetAxis() Axis
+TransformTo(Transform)
+TransformFrom(Transform)
}
class ShipTypeGroup {
<<abstract>>
Defines a set of ship types
+Rations Dictionary
+MinShip int
+CycleShip(int) int
}
class LineShipTypes {
Straight ship type config
+Rations Dictionary
+MinShip int
}
class ShipTypeManager {
MonoBehaviour, owns active
ship type group
+GetPrefab(int) ShipView
+Rations(int) int
+MinShip() int
+CycleShip(int) int
}
class ShipManager {
MonoBehaviour, handles
placement and selection
+SelectShip(int)
+AllShipsPlaced() bool
+LockPlacement()
+SetActiveBoard(bool)
}
class ShipPlacementUI {
UI buttons and labels
for ship placement
+UpdateButtons()
}

    Ship <|-- LineShip : extends
    ShipView <|-- LineShipView : extends
    ShipTypeGroup <|-- LineShipTypes : extends
    LineShipView --> LineShip : owns
    Ship --> AxisObject : uses
    ShipTypeManager --> ShipTypeGroup : holds
    ShipManager --> ShipTypeManager : uses
    ShipPlacementUI --> ShipTypeManager : reads rations
    ShipPlacementUI --> ShipManager : reads counts
```
