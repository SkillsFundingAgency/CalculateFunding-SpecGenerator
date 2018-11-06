﻿namespace CalculateFunding.Frontend.Clients.CommonModels
{
    using Newtonsoft.Json;

    public class Reference
    {
        public Reference()
        {
        }

        public Reference(string id, string name)
        {
            Id = id;
            Name = name;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}