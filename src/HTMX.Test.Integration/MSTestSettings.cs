using Microsoft.VisualStudio.TestTools.UnitTesting;

// Each test method runs on its own instance of its test class, and each instance starts the application with a
// database of its own, so tests can run side by side.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
