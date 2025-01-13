using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using _301279203_301283887_liyanage_raut__3.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Amazon.DynamoDBv2.DocumentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Amazon.DynamoDBv2.Model;
using System.Globalization;

namespace _301279203_301283887_liyanage_raut__3.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IDynamoDBContext _dynamoDbContext;
        private const string _s3bucketName = "movie-app-bucket-dr";
        private readonly MovieappContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IAmazonDynamoDB _dynamoDbClient;


        public MoviesController(IAmazonS3 s3Client, IAmazonDynamoDB dynamoDbClient, IDynamoDBContext dbContext, ILogger<HomeController> logger, MovieappContext context)
        {
            _s3Client = s3Client;
            _dynamoDbContext = dbContext;
            _dynamoDbClient = dynamoDbClient;
            _context = context;
            _logger = logger;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
			var userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Users");
			}

			List<ScanCondition> conditions = new List<ScanCondition>();
            if (userId.HasValue)
            {
                conditions.Add(new ScanCondition("UploaderId", ScanOperator.Equal, userId.Value));
            }

            var movies = await _dynamoDbContext.ScanAsync<Movie>(conditions).GetRemainingAsync();

            return View(movies);
        }

        public async Task<IActionResult> Dashboard()
        {
			var userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Users");
			}

			var movies = await _dynamoDbContext.ScanAsync<Movie>(new List<ScanCondition>()).GetRemainingAsync();

            return View(movies);
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(string id)
        {
			var userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Users");
			}

			var movie = await _dynamoDbContext.LoadAsync<Movie>(id);

            return movie == null ? NotFound() : View(movie);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
			var userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Users");
			}

			return View();
        }

		// GET: Movies/ViewMovie/5
		public async Task<IActionResult> ViewMovie(string id)
        {
			var userId = HttpContext.Session.GetInt32("UserId");

			if (userId == null)
			{
				return RedirectToAction("Login", "Users");
			}

			var movie = await _dynamoDbContext.LoadAsync<Movie>(id);

            if (movie == null)
            {
                return NotFound();
            }

            double averageRating = 0;

            if (movie.Comments != null && movie.Comments.Any())
            {
                averageRating = Math.Round((double)movie.Comments.Average(c => c.Rating), 1);
            }

            movie.Rating = averageRating;

            return View(movie);
        }

        private double ParseDouble(string rating)
        {
            if (double.TryParse(rating, out double result))
            {
                return result;
            }

            return 0;
        }

        [HttpGet]
        public async Task<IActionResult> SearchRating(string rating)
        {
            List<Movie> movies = new List<Movie>();

            try
            {
                if (!string.IsNullOrEmpty(rating))
                {
                    var queryRequest = new QueryRequest
                    {
                        TableName = "movie-tbl",
                        IndexName = "RatingIndex",
                        KeyConditionExpression = "Category = :category AND Rating >= :rating",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                        { ":category", new AttributeValue { S = "Movies" } },
                        { ":rating", new AttributeValue { N = rating } }
                    },

                    };
                    var queryResponse = await _dynamoDbClient.QueryAsync(queryRequest);

                    movies = queryResponse.Items.Select(item => new Movie
                    {
                        MovieId = item["MovieId"].S,
                        Director = item["Director"].S,
                        Title = item["Title"].S,
                        Rating = double.Parse(item["Rating"].N, CultureInfo.InvariantCulture),
                        Genre = item["Genre"].S,
                        ReleaseDate = item["ReleaseDate"].S
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error querying DynamoDB: {ex.Message}");
            }

            return View("Dashboard", movies);
        }

        [HttpGet]
        public async Task<IActionResult> SearchGenre(string genre)
        {
            List<Movie> movies = new List<Movie>();

            try
            {
                if (!string.IsNullOrEmpty(genre))
                {
                    var queryRequest = new QueryRequest
                    {
                        TableName = "movie-tbl",
                        IndexName = "GenreIndex",
                        KeyConditionExpression = "Category = :category AND Genre = :genre",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                        { ":category", new AttributeValue { S = "Movies" } },
                        { ":genre", new AttributeValue { S = genre } }
                    },

                    };
                    var queryResponse = await _dynamoDbClient.QueryAsync(queryRequest);

                    movies = queryResponse.Items.Select(item => new Movie
                    {
                        MovieId = item["MovieId"].S,
                        Title = item["Title"].S,
                        Director = item["Director"].S,
                        Rating = double.Parse(item["Rating"].N, CultureInfo.InvariantCulture),
                        Genre = item["Genre"].S,
                        ReleaseDate = item["ReleaseDate"].S
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error querying DynamoDB: {ex.Message}");
            }

            return View("Dashboard", movies);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int userId, string movieId, string comment, string rating)
        {
            double parsedRating = ParseDouble(rating);

            var movie = await _dynamoDbContext.LoadAsync<Movie>(movieId);

            if (movie == null)
            {
                return NotFound();
            }

			var user = await _context.Users.FirstOrDefaultAsync(m => m.UserId == userId);

            if (user == null)
            {
                return NotFound();
            }

            if (movie.Comments == null)
            {
                movie.Comments = new List<Comment>();
            }

            var existingComment = movie.Comments?.FirstOrDefault(c => c.UserId == userId);

            if (existingComment != null)
            {
                existingComment.Content = comment;
                existingComment.Rating = parsedRating;
            }
            else
            {
                var newComment = new Comment
                {
                    UserId = userId,
                    Content = comment,
                    Rating = parsedRating,
                    FullName = user.FullName
                };

                movie?.Comments?.Add(newComment);
            }

            if (movie.Comments.Any())
            {
                double currentAverage = Math.Round((double)movie.Comments.Average(c => c.Rating),1);

                movie.Rating = currentAverage;
            }

            await _dynamoDbContext.SaveAsync(movie);

            return RedirectToAction("ViewMovie", new { id = movieId });
        }

        [HttpPost]
        public async Task<IActionResult> EditComment(string commentId, int userId, string movieId, string content, string rating)
        {
            double parsedRating = ParseDouble(rating);

            var movie = await _dynamoDbContext.LoadAsync<Movie>(movieId);

            if (movie == null)
            {
                return NotFound();
            }

            var comment = movie.Comments?.FirstOrDefault(c => c.CommentId == commentId && c.UserId == userId);

            if (comment == null)
            {
                return NotFound("Comment not found or you do not have permission to edit this comment.");
            }

            comment.Content = content;
            comment.Rating = parsedRating;


            if (movie.Comments.Any())
            {
                double currentAverage = Math.Round((double)movie.Comments.Average(c => c.Rating), 1);

                movie.Rating = currentAverage;
            }

            await _dynamoDbContext.SaveAsync(movie);

            return RedirectToAction("ViewMovie", new { id = movieId });
        }



        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AddMovie addMovie)
        {
            var newMovieId = Guid.NewGuid().ToString();

            var uploadedUser = HttpContext.Session.GetInt32("UserId");

            Movie movie = new()
            {
                MovieId = newMovieId,
                Title = addMovie.Title,
                Genre = addMovie.Genre,
                Director = addMovie.Director,
                ReleaseDate = addMovie.ReleaseDate,
                UploaderId = uploadedUser
            };

            IFormFile? movieFile = addMovie.MovieUploadFile;

            if (movieFile != null && movieFile.Length > 0)
            {
                using var stream = movieFile.OpenReadStream();

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = newMovieId,
                    BucketName = _s3bucketName,
                    ContentType = movieFile.ContentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                try
                {
                    var fileTransferUtility = new TransferUtility(_s3Client);
                    await fileTransferUtility.UploadAsync(uploadRequest);

                    movie.S3Url = $"https://{_s3bucketName}.s3.amazonaws.com/{newMovieId}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error uploading file: " + ex.Message);
                    ModelState.AddModelError(string.Empty, "Unable to upload file. Please try again.");
                    return View(addMovie);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Please select a file to upload.");
                return View(addMovie);
            }

            if (!string.IsNullOrEmpty(addMovie.Title))
            {
                await _dynamoDbContext.SaveAsync(movie);
                return RedirectToAction(nameof(Index));
            }

            return View(addMovie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var movie = await _dynamoDbContext.LoadAsync<Movie>(id);
            if (movie == null)
            {
                return NotFound();
            }

            AddMovie editMovie = new()
            {
                Title = movie.Title,
                Genre = movie.Genre,
                Director = movie.Director,
                ReleaseDate = movie.ReleaseDate
            };

            ViewData["MovieId"] = id;

            return View(editMovie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, AddMovie updatedMovie)
        {
            if (id == "")
            {
                return NotFound();
            }

            var existingMovie = await _dynamoDbContext.LoadAsync<Movie>(id);
            if (existingMovie == null)
            {
                Console.WriteLine("movie not found ");
                return NotFound();
            }

            Console.WriteLine("movie found -> " + existingMovie.MovieId);

            existingMovie.Title = updatedMovie.Title;
            existingMovie.Genre = updatedMovie.Genre;
            existingMovie.Director = updatedMovie.Director;
            existingMovie.ReleaseDate = updatedMovie.ReleaseDate;

            // Check if a new file has been uploaded
            IFormFile? movieFile = updatedMovie.MovieUploadFile;

            if (movieFile != null && movieFile.Length > 0)
            {
                using var stream = movieFile.OpenReadStream();

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = stream,
                    Key = existingMovie.MovieId, // Use the existing MovieId to overwrite
                    BucketName = _s3bucketName,
                    ContentType = movieFile.ContentType,
                    CannedACL = S3CannedACL.PublicRead
                };

                try
                {
                    var fileTransferUtility = new TransferUtility(_s3Client);
                    await fileTransferUtility.UploadAsync(uploadRequest);

                    existingMovie.S3Url = $"https://{_s3bucketName}.s3.amazonaws.com/{existingMovie.MovieId}";
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error uploading file: " + ex.Message);
                    ModelState.AddModelError(string.Empty, "Unable to upload file. Please try again.");
                    return View(updatedMovie);
                }
            }

            // Save the updated movie information
            await _dynamoDbContext.SaveAsync(existingMovie);

            return RedirectToAction(nameof(Index));
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            var movie = await _dynamoDbContext.LoadAsync<Movie>(id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var movie = await _dynamoDbContext.LoadAsync<Movie>(id);
            if (movie == null)
            {
                return NotFound();
            }

            // Delete from S3
            try
            {
                await _s3Client.DeleteObjectAsync(_s3bucketName, movie.MovieId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deleting file from S3: " + ex.Message);
                ModelState.AddModelError(string.Empty, "Unable to delete file from S3. Please try again.");
                return View("Delete", movie);
            }

            // Delete from DynamoDB
            await _dynamoDbContext.DeleteAsync(movie);
            return RedirectToAction(nameof(Index));
        }
    }
}
