using System.Runtime.CompilerServices;

// The kit's API is internal on purpose — nothing outside this package should bind to the
// installer. The EditMode tests still need to reach it to check the skill contract.
[assembly: InternalsVisibleTo("AxuCliKit.Tests.Editor")]
