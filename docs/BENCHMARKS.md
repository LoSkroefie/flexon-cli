# Benchmark methodology

The repository intentionally does not publish universal speed or compression claims. Results depend on data shape, compression level, runtime, CPU, storage, warm-up, and whether integrity or encryption is enabled.

For a local smoke measurement:

```powershell
flexon-cli benchmark -i sample.json -o sample.flexon -c brotli
```

For publishable comparisons:

1. Pin the commit, SDK, operating system, CPU, power mode, and competitor versions.
2. Publish the raw datasets or deterministic generators.
3. Separate encoding, decoding, compression, encryption, and file I/O measurements.
4. Include warm-up, multiple iterations, variance, allocations, and peak memory.
5. Verify semantic round trips before timing them.
6. Compare equivalent safety settings and compression levels.
7. Publish raw results and commands, not only selected percentages.

The CLI benchmark is diagnostic, not a substitute for BenchmarkDotNet or a peer-reviewed performance study.
