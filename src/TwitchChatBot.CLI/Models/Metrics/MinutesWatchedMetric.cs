using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchChatBot.Shared.Models.Entities;
using TwitchChatBot.Shared.Models.Enums;

namespace TwitchChatBot.CLI.Models.Metrics
{
    public class MinutesWatchedMetric : IMetric
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public string Name { get; } = "LoggedInMinutesWatched";

        protected string Channel { get; private set; }
        protected IReadOnlyCollection<ChannelActivityEntity> Results { get; private set; }

        public MinutesWatchedMetric(string channel, DateTime start, DateTime end, IReadOnlyCollection<ChannelActivityEntity> results)
        {
            Channel = channel;
            Results = results;
            StartDate = start;
            EndDate = end;
        }


        public Task<double> Calculate()
        {
            var minutesWatchedLookup = new Dictionary<string, List<TimeSpan>>();
            var viewers = Results
                .Select(x => x.Viewer)
                .Where(x => !string.IsNullOrEmpty(x) && !string.Equals(x, Channel, StringComparison.InvariantCultureIgnoreCase))
                .Distinct();
            foreach (var viewer in viewers)
            {
                var events = Results
                    .Where(x => string.Equals(viewer, x.Viewer, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(x => x.Timestamp)
                    .ToList();

                var dateValue = events.FirstOrDefault(x => string.Equals(x.Activity, StreamActivity.UserJoined.ToString())) ??
                        events.OrderBy(x => x.Timestamp).FirstOrDefault();

                DateTime viewerStartDate = StartDate;
                if (dateValue != null && !string.IsNullOrEmpty(dateValue.RowKey))
                {
                    viewerStartDate = DateTime.Parse(dateValue.RowKey);
                }

                // Add users who may not have left before the stream ended
                if (!events.Any(x => x.Activity == StreamActivity.UserLeft.ToString()))
                {
                    var tempMinutesWatched = EndDate.Subtract(viewerStartDate);
                    minutesWatchedLookup.Add(viewer, new List<TimeSpan> { tempMinutesWatched });
                }
                else
                {
                    var userActivities = events
                        .OrderBy(x => x.Timestamp)
                        .Where(x => string.Equals(x.Activity, StreamActivity.UserJoined.ToString()) || string.Equals(x.Activity, StreamActivity.UserLeft.ToString()))
                        .Select(x => new
                        {
                            Activity = (StreamActivity)Enum.Parse(typeof(StreamActivity), x.Activity),
                            Timestamp = DateTime.Parse(x.RowKey)
                        })
                        .ToList();

                    // if first event if UserLeft, set the userStart to the streamstart
                    var firstActivity = userActivities.FirstOrDefault();
                    if (firstActivity != null && firstActivity.Activity == StreamActivity.UserLeft)
                    {
                        var difference = userActivities.First().Timestamp.Subtract(StartDate);
                        if (minutesWatchedLookup.ContainsKey(viewer))
                        {
                            var currentMinutesWatchedList = minutesWatchedLookup[viewer];
                            currentMinutesWatchedList.Add(difference);
                        }
                        else
                        {
                            minutesWatchedLookup.Add(viewer, new List<TimeSpan> { difference });
                        }
                        userActivities.Remove(firstActivity);
                    }

                    // Finds all userJoined events and their closest corresponding UserLeft events
                    foreach (var entry in userActivities.Where(x => x.Activity == StreamActivity.UserJoined))
                    {
                        var exit = userActivities.FirstOrDefault(x => x.Activity == StreamActivity.UserLeft && x.Timestamp >= entry.Timestamp);
                        if (exit == null)
                        {
                            exit = new
                            {
                                Activity = StreamActivity.UserLeft,
                                Timestamp = EndDate
                            };
                        }
                        var difference = exit.Timestamp.Subtract(entry.Timestamp);

                        if (minutesWatchedLookup.ContainsKey(viewer))
                        {
                            var currentMinutesWatchedList = minutesWatchedLookup[viewer];
                            currentMinutesWatchedList.Add(difference);
                        }
                        else
                        {
                            minutesWatchedLookup.Add(viewer, new List<TimeSpan> { difference });
                        }
                    }

                }
            }
            var minutesWatched = minutesWatchedLookup.Values.Sum(x => x.Sum(y => y.TotalMinutes));

            return Task.FromResult(minutesWatched);
        }
    }
}
