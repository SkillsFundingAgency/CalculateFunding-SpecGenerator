using System;

namespace CalculateFunding.Frontend.Clients.DatasetsClient.Models
{
    public class DatasetValidationStatusModel
    {
        public string OperationId { get; set; }

        public DatasetValidationStatus CurrentOperation { get; set; }

        public string ErrorMessage { get; set; }

        public DateTimeOffset LastUpdated { get; set; }

        public DatasetCreateUpdateResponseModel UpdateResult { get; set; }
    }
}
