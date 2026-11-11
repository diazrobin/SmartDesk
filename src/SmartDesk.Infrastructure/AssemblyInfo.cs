using System.Runtime.CompilerServices;

// Solo el proyecto de tests de integración puede ver los tipos internos de Infrastructure
[assembly: InternalsVisibleTo("SmartDesk.Integration.Tests")]
