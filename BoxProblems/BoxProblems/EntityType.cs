using System;
using System.Collections.Generic;
using System.Text;

namespace BoxProblems
{
    [Flags]
    internal enum EntityType
    {
        AGENT       = 0b0000_0001,
        BOX         = 0b0000_0010,
        MOVEABLE    = AGENT | BOX,
        AGENT_GOAL  = 0b0000_0100,
        BOX_GOAL    = 0b0000_1000,
        GOAL        = AGENT_GOAL | BOX_GOAL
    }
}
