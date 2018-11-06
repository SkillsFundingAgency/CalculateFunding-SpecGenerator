﻿namespace CalculateFunding.Frontend.Clients.ResultsClient.Models
{
    using System;

    public class CalculationProviderResultSearchResultItem : ProviderSearchResultItem
    {
        public DateTimeOffset? LastUpdatedDate { get; set; }

        public Decimal CalculationResult { get; set; }

        public string CalculationType { get; set; }
    }
}
