using System.Text;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MyGallary.Models;

namespace MyGallary.Controllers;

public class PhotoController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly BlobContainerClient _blobContainer;
    
    public PhotoController(IConfiguration configuration)
    {
        _configuration = configuration;

        string? connectionString = _configuration["BlobStorage:ConnectionString"];
        string? containerName = _configuration["BlobStorage:ContainerName"];

        _blobContainer = new BlobContainerClient(connectionString, containerName);
    }

    public IActionResult Index(string searchQuery)
    {
        List<Photo> photos = new();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            char[] separators = new[] { ',', '"' };
            string[] tags = searchQuery.Split(separators).Select(p => p.Trim().ToLower()).ToArray();
            
            foreach (var blobItem in _blobContainer.GetBlobs())
            {
                var blobClient = _blobContainer.GetBlobClient(blobItem.Name);

                var metadata = blobClient.GetProperties().Value.Metadata;
                string[] metaTags = metadata["Tags"].Split(separators).Select(p => p.Trim().ToLower()).ToArray();
                bool containsTaggedElement = tags.Any(tag => metaTags.Contains(tag));

                if (containsTaggedElement)
                {
                    photos.Add(new Photo()
                    {
                        Id = blobItem.Name,
                        Name = metadata.ContainsKey("Name") ? metadata["Name"] : string.Empty,
                        Description = metadata.ContainsKey("Description") ? metadata["Description"] : string.Empty,
                        Tags = metadata.ContainsKey("Tags") ? metadata["Tags"] : string.Empty,
                        Type = metadata.ContainsKey("Type") ? metadata["Type"] : String.Empty,
                        DateOfUpload = metadata.ContainsKey("DateOfUpload") ? metadata["DateOfUpload"] : DateTime.Today.ToShortDateString(),
                        ImageUrl = blobClient.Uri.AbsoluteUri
                    });
                }
            }

            ViewData["Search"] = searchQuery;
        }
        else
        {
            foreach (var blobItem in _blobContainer.GetBlobs())
            {
                var blobClient = _blobContainer.GetBlobClient(blobItem.Name);

                var metadata = blobClient.GetProperties().Value.Metadata;

                photos.Add(new Photo()
                {
                    Id = blobItem.Name,
                    Name = metadata.ContainsKey("Name") ? metadata["Name"] : string.Empty,
                    Description = metadata.ContainsKey("Description") ? metadata["Description"] : string.Empty,
                    Tags = metadata.ContainsKey("Tags") ? metadata["Tags"] : string.Empty,
                    Type = metadata.ContainsKey("Type") ? metadata["Type"] : String.Empty,
                    DateOfUpload = metadata.ContainsKey("DateOfUpload") ? metadata["DateOfUpload"] : DateTime.Today.ToShortDateString(),
                    ImageUrl = blobClient.Uri.AbsoluteUri
                });
            }

            ViewData["Search"] = "";
        }

        photos = photos.OrderByDescending(p => Convert.ToDateTime(p.DateOfUpload)).ToList();

        return View(photos);
    }

    [HttpGet]
    public IActionResult AddPhoto()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddPhoto(AddPhotoDto model, IFormFile file)
    {
        if (file != null && file.Length > 0 && ModelState.IsValid)
        {
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = _blobContainer.GetBlobClient(fileName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }
            
            var metadata = new Dictionary<string, string>
            {
                { "Name", Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(model.Name))},
                { "Description", Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(model.Description)) },
                { "Tags", Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(model.Tags)) },
                { "Type", Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(new FileInfo(fileName).Extension)) },
                {"DateOfUpload", Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(DateTime.Now.ToShortDateString()))}
            };
            
            await blobClient.SetMetadataAsync(metadata);

            return RedirectToAction("Index");
        }
        
        // ModelState.AddModelError(string.Empty, "Please select a file.");
        return View(model);
    }

    public IActionResult ViewPhoto(string id)
    {
        var blobClient = _blobContainer.GetBlobClient(id);
        var metadata = blobClient.GetProperties().Value.Metadata;

        var photo = new Photo
        {
            Id = id,
            Name = metadata.ContainsKey("Name") ? metadata["Name"] : string.Empty,
            Description = metadata.ContainsKey("Description") ? metadata["Description"] : string.Empty,
            Tags = metadata.ContainsKey("Tags") ? metadata["Tags"] : string.Empty,
            Type = metadata.ContainsKey("Type") ? metadata["Type"] : String.Empty,
            DateOfUpload = metadata.ContainsKey("DateOfUpload") ? metadata["DateOfUpload"] : DateTime.Today.ToShortDateString(),
            ImageUrl = blobClient.Uri.AbsoluteUri
        };
        
        return View(photo);
    }

    public async Task<IActionResult> DeleteBlob(string id)
    {
        var blob = _blobContainer.GetBlobClient(id);
        if (blob.Exists())
        {
            await blob.DeleteAsync();
        }
        else
        {
            throw new Exception("File is not existed");
        }
       
        return RedirectToAction("Index");
    }

    public IActionResult Download(string id)
    {
        var blobClient = _blobContainer.GetBlobClient(id);
        var downloadStream = new MemoryStream();

        blobClient.DownloadTo(downloadStream);
        downloadStream.Position = 0;

        return File(downloadStream, blobClient.GetProperties().Value.ContentType, id);
    }

    public IActionResult Copy(string id)
    {
        var blobClient = _blobContainer.GetBlobClient(id);
        var copyBlobClient = _blobContainer.GetBlobClient("Copy_" + id);

        copyBlobClient.StartCopyFromUri(blobClient.Uri);

        return RedirectToAction("Index");
    }
}