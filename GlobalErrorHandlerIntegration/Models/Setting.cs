namespace GlobalErrorHandlerIntegration.Models
{
    public class Setting
    {
        public string Label { get; set; }
        public string Type { get; set; }
        public string? description { get; set; }
        public bool? Is_required { get; set; }
        public object Default { get; set; }
    }
}
