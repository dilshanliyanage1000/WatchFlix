using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Amazon.DynamoDBv2.DataModel;
using Humanizer.Localisation;

namespace _301279203_301283887_liyanage_raut__3.Models
{
    [DynamoDBTable("movie-tbl")]
    public class Movie
    {
        [DynamoDBHashKey]
        public string MovieId { get; set; }

        [DynamoDBProperty]
        public string? Title { get; set; }

        [DynamoDBProperty]
        public string? Genre { get; set; }

        [DynamoDBProperty]
        public string? Director { get; set; }

        [DynamoDBProperty]
        public string? ReleaseDate { get; set; }

        [DynamoDBProperty]
        public string? S3Url { get; set; }

        [DynamoDBProperty]
        public List<Comment>? Comments { get; set; } = new List<Comment>();

        [DynamoDBProperty]
        public string? Category { get; set; }

        [DynamoDBProperty]
        public double? Rating { get; set; }

        [DynamoDBProperty]
        public int? UploaderId { get; set; }

        public Movie()
        {
            MovieId = Guid.NewGuid().ToString();
            Category = "Movies";
            Rating = 0;
        }
    }
    public class Comment
    {
        public string? CommentId { get; set; }
        public int? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Content { get; set; }
        public double? Rating { get; set; }
        public DateTime? PostedAt { get; set; }

        public Comment()
        {
            CommentId = Guid.NewGuid().ToString();
            PostedAt = DateTime.Now;
        }
    }


}
