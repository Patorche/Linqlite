using System;
using System.Collections.Generic;
using System.Text;

namespace Linqlite.Linq
{
    internal interface IQueryableTableDefinition
    {
        Type EntityType { get; }
        TrackingMode? TrackingModeOverride { get; }
    }
}
