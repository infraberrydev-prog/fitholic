namespace FitHolic.Models
{
    public class PackageDto
    {
        public int Id { get; set; }
        public string Package {  get; set; }
        public string Price {  get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
