using Xunit;

// The BDD scenarios each start the multiprotocol server on fixed ports
// (8080/8081/8082), so scenarios must not run concurrently across classes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
