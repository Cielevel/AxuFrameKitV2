# NextGen依赖顺序
```mermaid
graph LR
    A[_Core]
    B[Common]
    C[_DevUtils]
    
    B ==依赖核心==> A
    C ==依赖核心==> A
    C --依赖Kit--> B
```