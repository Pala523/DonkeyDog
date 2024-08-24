using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DonkeyDog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly GridFSBucket _gridFSBucket;
        private readonly IMongoCollection<BsonDocument> _filesCollection;

        public ImagesController(GridFSBucket gridFSBucket, IMongoDatabase database)
        {
            _gridFSBucket = gridFSBucket;
            _filesCollection = database.GetCollection<BsonDocument>("fs.files");
        }

        // Endpoint per caricare un'immagine con metadati
        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string info, [FromForm] string address, [FromForm] string date, [FromForm] string title, [FromForm] string author = null, [FromForm] string link = null)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var metadata = new BsonDocument
    {
        { "info", info },
        { "address", address },
        { "date", date },
        { "title", title }
    };

            if (!string.IsNullOrEmpty(author))
            {
                metadata.Add("author", author);
            }

            if (!string.IsNullOrEmpty(link))
            {
                metadata.Add("link", link);
            }

            using (var stream = file.OpenReadStream())
            {
                var fileId = await _gridFSBucket.UploadFromStreamAsync(file.FileName, stream, new GridFSUploadOptions { Metadata = metadata });
                return Ok(new { FileId = fileId.ToString() });
            }
        }

        // Endpoint per scaricare un'immagine
        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> Download(string fileId)
        {
            if (!ObjectId.TryParse(fileId, out var id))
            {
                return BadRequest("Invalid file ID.");
            }

            // Recupera i metadati del file dalla collezione fs.files
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var file = await _filesCollection.Find(filter).FirstOrDefaultAsync();

            if (file == null)
            {
                return NotFound("File not found.");
            }

            var fileName = file.GetValue("filename", "unknown").AsString;
            var contentType = file.GetValue("contentType", "application/octet-stream").AsString;

            // Apri lo stream di download del file
            var downloadStream = await _gridFSBucket.OpenDownloadStreamAsync(id);

            return File(downloadStream, contentType, fileName);
        }

        // Endpoint per ottenere tutti i file con metadati
        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            var files = await _filesCollection.Find(new BsonDocument()).ToListAsync();

            var fileList = new List<object>();
            foreach (var file in files)
            {
                fileList.Add(new
                {
                    Id = file["_id"].AsObjectId.ToString(),
                    FileName = file.GetValue("filename", "unknown").AsString,
                    ContentType = file.GetValue("contentType", "application/octet-stream").AsString,
                    UploadDate = file.GetValue("uploadDate", BsonNull.Value) != BsonNull.Value ? file["uploadDate"].ToUniversalTime() : (DateTime?)null,
                    Info = file.Contains("metadata") && file["metadata"].AsBsonDocument.Contains("info") ? file["metadata"]["info"].AsString : null,
                    Address = file.Contains("metadata") && file["metadata"].AsBsonDocument.Contains("address") ? file["metadata"]["address"].AsString : null,
                    Date = file.Contains("metadata") && file["metadata"].AsBsonDocument.Contains("date") ? file["metadata"]["date"].AsString : null,
                    Title = file.Contains("metadata") && file["metadata"].AsBsonDocument.Contains("title") ? file["metadata"]["title"].AsString : null,
                    Author = file.Contains("metadata") && file["metadata"].AsBsonDocument.Contains("author") ? file["metadata"]["author"].AsString : null,
                    Link = file.Contains("metadata") && file["metadata"].AsBsonDocument.Contains("link") ? file["metadata"]["link"].AsString : null
                });
            }

            return Ok(fileList);
        }
        [Authorize]
        [HttpDelete("delete/{fileId}")]
        public async Task<IActionResult> Delete(string fileId)
        {
            if (!ObjectId.TryParse(fileId, out var id))
            {
                return BadRequest("Invalid file ID.");
            }

            // Delete the file from GridFS
            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var file = await _filesCollection.Find(filter).FirstOrDefaultAsync();

            if (file == null)
            {
                return NotFound("File not found.");
            }

            await _gridFSBucket.DeleteAsync(id);

            return Ok("File deleted successfully.");
        }

        // Endpoint to update an image's metadata
        [Authorize]
        [HttpPut("update/{fileId}")]
        public async Task<IActionResult> UpdateMetadata(string fileId, [FromForm] string info, [FromForm] string address, [FromForm] string date, [FromForm] string title, [FromForm] string author = null, [FromForm] string link = null)
        {
            if (!ObjectId.TryParse(fileId, out var id))
            {
                return BadRequest("Invalid file ID.");
            }

            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var file = await _filesCollection.Find(filter).FirstOrDefaultAsync();

            if (file == null)
            {
                return NotFound("File not found.");
            }

            var updateDefinition = Builders<BsonDocument>.Update
                .Set("metadata.info", info)
                .Set("metadata.address", address)
                .Set("metadata.date", date)
                .Set("metadata.title", title);

            if (!string.IsNullOrEmpty(author))
            {
                updateDefinition = updateDefinition.Set("metadata.author", author);
            }

            if (!string.IsNullOrEmpty(link))
            {
                updateDefinition = updateDefinition.Set("metadata.link", link);
            }

            var updateResult = await _filesCollection.UpdateOneAsync(filter, updateDefinition);

            if (updateResult.ModifiedCount == 0)
            {
                return StatusCode(500, "Failed to update metadata.");
            }

            return Ok("Metadata updated successfully.");
        }

    }
}
