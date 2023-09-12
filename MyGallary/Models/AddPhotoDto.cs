using System.ComponentModel.DataAnnotations;

namespace MyGallary.Models;

public class AddPhotoDto
{
    [Required]
    [MaxLength(34)]
    public string Name { get; set; }
    [Required]
    public string Description { get; set; }
    [Required]
    public string Tags { get; set; }
}