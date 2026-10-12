using Xunit;

// WinForms controls, Syncfusion theming and GDI+ share process-wide state that
// is not thread-safe. Running UI test collections in parallel intermittently
// corrupted that state, causing flaky assertions and, occasionally, a
// test-host stack overflow. Serial execution keeps the suite deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
