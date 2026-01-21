# ReadMe

此 FSM 源于第三方代码；作者项目 git 链接：

- [github-3D platformer](https://github.com/adammyhre/3D-Platformer)

## 说明

此 FSM 为有限状态机的抽象，相较于使用 enum 管理状态的确认、转移和识别等，该 FSM 使用**强关系模式**（作者原话是：使用了策略模式）。

- state 对 state 的转移有确切的关系、转移条件。
- state 之间的关系类似于图/树。
- state 的转移更像 unity 中的 animator（可以参考 animator 来进行理解）。
