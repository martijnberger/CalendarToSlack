using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Exchange.WebServices.Data;

namespace CalendarToSlack
{
    class UserDatabase
    {
        private readonly List<RegisteredUser> _registeredUsers = new List<RegisteredUser>();

        public void Load(string file)
        {
            if (!File.Exists(file))
            {
                File.Create(file);
                return;
            }

            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                if (line.StartsWith("#"))
                {
                    continue;
                }

                var fields = line.Split(',');
                var user = new RegisteredUser
                {
                    ExchangeUsername = fields[0],
                    SlackApplicationAuthToken = fields[1],
                    HackyPersonalFullAccessSlackToken = fields[2],
                };
                Out.WriteDebug("Loaded registered user {0}", user.ExchangeUsername);
                _registeredUsers.Add(user);
            }
        }

        public void QueryAndSetSlackUserInfo(Slack slack)
        {
            // Hacky - first user's creds are used to list all users.
            var authToken = _registeredUsers[0].SlackApplicationAuthToken;

            var slackUsers = slack.ListUsers(authToken);
            Out.WriteDebug("Found {0} slack users", slackUsers.Count);

            foreach (var user in _registeredUsers)
            {
                var email = user.ExchangeUsername;
                var userInfo = slackUsers.FirstOrDefault(u => u.Email == email);
                if (userInfo != null)
                {
                    user.SlackUserInfo = userInfo;
                    Out.WriteDebug("Associated Exchange user {0} with Slack User {1} {2} {3} {4}",
                        email, userInfo.UserId, userInfo.Username, userInfo.FirstName, userInfo.LastName);
                }
                else
                {
                    Out.WriteInfo("Couldn't find Slack user with email {0}", email);
                }
                
            }
        }

        public List<RegisteredUser> Users
        {
            get { return _registeredUsers; }
        }
    }

    // TODO change last name if it needs changing (even if status remains the same)?

    class RegisteredUser
    {
        public string ExchangeUsername { get; set; }
        public string SlackApplicationAuthToken { get; set; }
        public string HackyPersonalFullAccessSlackToken { get; set; } // Will be removed.

        // These fields aren't persisted, but get set/modified during runtime.
        public CalendarEvent CurrentEvent { get; set; }
        public SlackUserInfo SlackUserInfo { get; set; }
    }
}