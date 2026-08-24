namespace FitHolic.DTO
{
    public class UpdateSavingSuccessfulResponse
    {
        public string message { get; set; } = string.Empty;
        public List<int> savedIds { get; set; } = new();
    }
}
