using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace SeriousSourceGenerator.TestApp;

public partial class SampleContainer {
    [GeneratedRegex(@"^[a-zA-Z]+[0-9]*?|[0-9]*?[a-zA-Z]+$")]
    public partial Regex GetGeneratedRegex();

    [GeneratedRegex(@"^[a-zA-Z]+[0-9]*?|[0-9]*?[a-zA-Z]+$")]
    public partial Regex GeneratedRegex { get; }

    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Debug,
        Message = "The specified giraffe is too long: Length: `{GiraffeLength}`")]
    public static partial void GiraffeTooLong(ILogger logger, Int32 giraffeLength);
}
