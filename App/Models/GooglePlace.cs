using Newtonsoft.Json;
using System.Collections.Generic;

namespace App.Models
{
    public class GooglePlaceResponse
    {
        [JsonProperty("results")]
        public List<GooglePlace> Results { get; set; } = new();

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("error_message")]
        public string ErrorMessage { get; set; } = string.Empty;

        [JsonProperty("html_attributions")]
        public List<object> HtmlAttributions { get; set; } = new();

        [JsonProperty("next_page_token")]
        public string NextPageToken { get; set; } = string.Empty;
    }

    public class GooglePlace
    {
        [JsonProperty("name")]
        public string Nombre { get; set; } = string.Empty;

        [JsonProperty("vicinity")]
        public string Direccion { get; set; } = string.Empty;

        [JsonProperty("geometry")]
        public Geometry Geometry { get; set; } = new();

        [JsonProperty("place_id")]
        public string PlaceId { get; set; } = string.Empty;

        [JsonProperty("rating", NullValueHandling = NullValueHandling.Ignore)]
        public double? Rating { get; set; }

        [JsonProperty("user_ratings_total", NullValueHandling = NullValueHandling.Ignore)]
        public int? UserRatingsTotal { get; set; }

        [JsonProperty("opening_hours", NullValueHandling = NullValueHandling.Ignore)]
        public OpeningHours OpeningHours { get; set; } = new();

        [JsonProperty("business_status")]
        public string BusinessStatus { get; set; } = string.Empty;
    }

    public class Geometry
    {
        [JsonProperty("location")]
        public LocationData Location { get; set; } = new();
    }

    public class LocationData
    {
        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lng")]
        public double Lng { get; set; }
    }

    public class OpeningHours
    {
        [JsonProperty("open_now")]
        public bool? OpenNow { get; set; }
    }
}


