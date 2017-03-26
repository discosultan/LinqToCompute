# LinqToCompute

Experimental Vulkan compute provider for .NET LINQ. The purpose of this project is to evaluate the capabilities and potential pitfalls of such a provider.

The entire public API consists of a single extension method `AsComputeQuery` to `IEnumerable{T}`.

```csharp
int[] input = Enumerable.Range(0, 200_000_000).ToArray();

input.Select(x => x + 1).ToArray();                  // 3.098s CPU single core
input.AsParallel().Select(x => x + 1).ToArray();     // 1.290s CPU multiple cores
input.AsComputeQuery().Select(x => x + 1).ToArray(); // 0.954s GPU
```

# Related Work

- [GpuLinq](https://github.com/nessos/GpuLinq)
- [Brahma](https://brahma.codeplex.com/)
