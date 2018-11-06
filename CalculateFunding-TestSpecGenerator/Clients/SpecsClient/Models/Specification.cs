﻿namespace CalculateFunding.Frontend.Clients.SpecsClient.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using CalculateFunding.Frontend.Clients.CommonModels;
    using Newtonsoft.Json;

    public class Specification : Reference
    {
        public Specification()
        {
            Policies = Enumerable.Empty<Policy>();
        }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("policies")]
        public IEnumerable<Policy> Policies { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<FundingStream> FundingStreams { get; set; }

        [JsonProperty("publishStatus")]
        public PublishStatus PublishStatus { get; set; }

        [JsonProperty("isSelectedForFunding")]
        public bool IsSelectedForFunding { get; set; }

    }
}