﻿using CalculateFunding.Frontend.Clients.CommonModels;
using System;

namespace CalculateFunding.Frontend.Clients.ResultsClient.Models.Results
{
    public class ScenarioResultItem
    {
        public Reference Scenario { get; set; }

        public string Result { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
