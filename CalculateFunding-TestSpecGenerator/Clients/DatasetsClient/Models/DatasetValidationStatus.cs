namespace CalculateFunding.Frontend.Clients.DatasetsClient.Models
{
    public enum DatasetValidationStatus
    {
        Queued,
        Processing,
        ValidatingExcelWorkbook,
        ValidatingTableResults,
        SavingResults,
        Validated,
        FailedValidation,
        ExceptionThrown,
    }
}
