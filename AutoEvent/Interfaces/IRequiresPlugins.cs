using System.Collections.Generic;

namespace AutoEvent.Interfaces;

public interface IRequiresPlugins
{
    IReadOnlyList<string> RequiredPlugins { get; }
    IReadOnlyList<string> RequiredDependencies { get; }
}