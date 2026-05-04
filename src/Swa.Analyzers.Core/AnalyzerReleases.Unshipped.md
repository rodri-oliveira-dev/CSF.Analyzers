### New Rules

| Rule ID | Category    | Severity | Notes                                                    |
| ------- | ----------- | -------- | -------------------------------------------------------- |
| ARCH001 | Reliability | Warning  | Avoid async void outside standard event handlers.        |
| ARCH002 | Reliability | Warning  | Avoid Task.ContinueWith. Prefer await.                   |
| ARCH003 | TestQuality | Info     | Avoid FluentAssertions NotBeNull() in tests.             |
| ARCH004 | TestQuality | Info     | Enforce _sut naming for the system under test.           |
| ARCH005 | TestQuality | Info     | Restrict NSubstitute Arg.Any() usage.                    |
| ARCH006 | TestQuality | Info     | Warn on FluentAssertions exclusions in BeEquivalentTo.   |
| ARCH007 | Performance | Info     | Detect string concatenation inside loops.                |
| ARCH008 | Reliability | Info     | Prohibit manual path composition in filesystem sinks.    |
| ARCH009 | Reliability | Warning  | Prohibit synchronous blocking of async operations.       |
| ARCH010 | Reliability | Warning  | Enforce CancellationToken propagation for async infrastructure calls. |
| ARCH011 | Reliability | Warning  | Prohibit asynchronous or blocking logic in constructors. |
| ARCH012 | Reliability | Info     | Prefer DateTimeOffset over DateTime.                     |
| ARCH013 | TestQuality | Info     | Restrict mocking frameworks to NSubstitute.              |
| ARCH014 | TestQuality | Info     | Prefer Is.Equivalent over NSubstitute Arg.Is.            |
| ARCH015 | Design      | Warning  | Prohibit verbs in HTTP route segments.                   |
| ARCH016 | Performance | Warning  | Avoid Task.Run in ASP.NET request flows.                 |
| ARCH017 | Reliability | Warning  | Prohibit fire-and-forget in ASP.NET request flows.       |
| ARCH018 | Reliability | Warning  | Avoid direct HttpClient instantiation.                   |
| ARCH019 | Security    | Warning  | Avoid conflicting Authorize and AllowAnonymous metadata. |
| ARCH020 | Security    | Warning  | Require explicit authorization decision on HTTP endpoints. |
| ARCH021 | Performance | Warning  | Prefer AsNoTracking for read-only EF Core queries.       |
| ARCH022 | Performance | Warning  | Avoid premature query materialization before filtering or projection. |
| ARCH023 | Testability | Warning  | Prefer TimeProvider over direct system clock access.     |
| ARCH024 | Observability | Warning  | Avoid interpolated strings or concatenation in ILogger calls. |
| ARCH025 | Observability | Warning  | Enforce ILogger category matching the containing type.   |
| ARCH026 | Security    | Warning  | Avoid insecure ASP.NET Core CORS configuration.          |
| ARCH027 | Architecture | Warning  | Prevent infrastructure dependencies in core layers.      |
| ARCH028 | Design      | Warning  | Prohibit mutable properties in records.                  |
| ARCH029 | Design      | Warning  | Prohibit public setters in domain entities.              |
| ARCH030 | Maintainability | Info     | Detect duplicated PackageReference items across projects. |
| ARCH031 | Performance | Warning  | Prefer System.Threading.Lock over object lock monitors.  |
| ARCH032 | Maintainability | Info     | Avoid duplicated MSBuild properties between project files and Directory.Build.props. |
| ARCH033 | Reliability | Warning  | Avoid BuildServiceProvider during service registration.  |
