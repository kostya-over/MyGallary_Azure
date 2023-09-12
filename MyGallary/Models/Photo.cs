using System.ComponentModel.DataAnnotations;

namespace MyGallary.Models;

public class Photo
{
    [Key]
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public string Tags { get; set; }
    public string Type { get; set; }
    public string? DateOfUpload { get; set; } = DateTime.Now.ToShortDateString();
    public string? ImageUrl { get; set; }
}