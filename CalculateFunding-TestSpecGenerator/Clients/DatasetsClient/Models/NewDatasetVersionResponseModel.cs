﻿namespace CalculateFunding.Frontend.Clients.DatasetsClient.Models
{
    using CalculateFunding.Frontend.Clients.CommonModels;

    public class NewDatasetVersionResponseModel : CreateNewDatasetModel
    {
        public string BlobUrl { get; set; }

        public string DatasetId { get; set; }

        public Reference Author { get; set; }

        public int Version { get; set; }
    }
}
