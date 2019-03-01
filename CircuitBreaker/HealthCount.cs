﻿using System;

namespace CircuitBreaker
{
    [Serializable]
    public class HealthCount
    {
        public int Successes { get; set; }
        public int Failures { get; set; }
        public int Total { get { return Successes + Failures; } }
        public long StartedAt { get; set; }

        public HealthCount()
        {
            //StartedAt = DateTime.UtcNow.Ticks;
        }
    }
}