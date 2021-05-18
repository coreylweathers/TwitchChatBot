using System;
using System.Text.Json.Serialization;

namespace TwitchChatBot.Client.Models.Twitch
{
  /*
   * {
"data": [
  {
    "user_id": "423374343",
    "user_login": "glowillig",
    "user_name": "glowillig",
    "expires_at": "2019-03-15T02:00:28Z"
  },
  {
    "user_id": "424596340",
    "user_login": "quotrok",
    "user_name": "quotrok",
    "expires_at": "2018-08-07T02:07:55Z"
  },
  ...
],
"pagination": {
  "cursor": "eyJiIjpudWxsLCJhIjp7IkN1cnNvciI6IjEwMDQ3MzA2NDo4NjQwNjU3MToxSVZCVDFKMnY5M1BTOXh3d1E0dUdXMkJOMFcifX0"
}
}
   */

  public class TwitchBanResponse
  {
    public TwitchBannedUser[] Data { get; set; }
    public Pagination Pagination { get; set; }
  }

  public class TwitchBannedUser
  {
    [JsonPropertyName("user_id")] public string UserId { get; set; }
    [JsonPropertyName("user_login")] public string UserLogin { get; set; }
    [JsonPropertyName("user_name")] public string UserName { get; set; }
    [JsonPropertyName("expires_at")] public DateTime ExpiresAt { get; set; }
  }

  public class Pagination
  {
    public string Cursor { get; set; }
  }

}