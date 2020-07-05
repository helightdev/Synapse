﻿using Synapse.Api;

namespace Synapse.Events.Classes
{
    public class FemurEnterEvent
    {
        public Player Player { get; internal set; }

        public bool Allow { get; set; }

        public bool CloseFemur { get; set; }
    }
}
