using Amazon.DynamoDBv2.DataModel;

namespace _301279203_301283887_liyanage_raut__3.Models
{
    public class AddMovie
    {
        public string? AddMovieId { get; set; }

        public string? Title { get; set; }

        public string? Genre { get; set; }

        public string? Director { get; set; }

        public string? ReleaseDate { get; set; }

        public IFormFile? MovieUploadFile { get; set; }

        public AddMovie()
        {
            AddMovieId = Guid.NewGuid().ToString();
        }
    }
}
