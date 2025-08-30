using System;
using System.Collections.Generic;

namespace BussinessCupApi.Models.Dtos
{
    public class MatchNewsDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string MatchNewsMainPhoto { get; set; }
        public string DetailsTitle { get; set; }
        public string Details { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<MatchNewsPhotoDto> Photos { get; set; }
    }
    public class MatchNewsPhotoDto
    {
        public int Id { get; set; }
        public string PhotoUrl { get; set; }
    }
}