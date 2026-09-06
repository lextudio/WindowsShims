using Xunit;

// Uno's DependencyProperty registry is not thread-safe on first registration:
// DependencyProperty.RegisterAttached goes through Uno.Collections.HashtableEx, whose
// rehash throws IndexOutOfRangeException when two test classes first touch different
// DP-owning types at the same moment. Real apps register on the UI thread, so this is a
// harness problem rather than a product one — but it makes the suite flaky, and which
// tests collide depends on how many classes there are. Run the assembly serially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
