using Xunit;

// Several test classes exercise JordiumSnowflakeIDGeneratorFactory.Default, which is a
// process-wide static singleton. Running test classes in parallel (xUnit's default
// behavior) can cause races between InitializeDefault/ResetDefault calls across classes.
// Disabling test parallelization keeps the shared static state deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
