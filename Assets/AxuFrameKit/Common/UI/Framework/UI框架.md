# UI框架
层级图：
```mermaid
flowchart TD
    UIManager["UIManager"]     
    UIPage["UIPage"]
    UIArea["UIArea"]
    UIPanel["UIPanel"]
    UIUnit["UIUnit"]
    
    UIManager --> UIPage
    UIPage --> UIArea
    UIArea --> UIPanel
    UIPanel --> UIUnit
```