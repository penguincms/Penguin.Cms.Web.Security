using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Penguin.Cms.Security;
using Penguin.Cms.Security.Constants;
using Penguin.Extensions.Collections;
using Penguin.Messaging.Abstractions.Interfaces;
using Penguin.Messaging.Persistence.Messages;
using Penguin.Security.Abstractions.Interfaces;
using Penguin.Web.Extensions;
using System;
using System.Collections.Concurrent;

namespace Penguin.Cms.Web.Security
{
    [Serializable]
    public class UserSession : IUserSession, IMessageHandler<Updated<User>>
    {
        private static readonly ConcurrentDictionary<int, string> UserCache = new ConcurrentDictionary<int, string>();
        private readonly ISession Session;
        private string CachedSessionUser;
        public bool AllowNSFW { get => this.Session?.Get<bool>("AllowNsfw") ?? false; set => this.Session?.Set("AllowNsfw", value); }

        public bool IsLocalConnection { get; set; }
        public bool IsLoggedIn => this.Session != null && this.Session.Get<int>("LoggedInUserId") != 0;

        public User LoggedInUser
        {
            get
            {
                User toReturn = JsonConvert.DeserializeObject<User>(this.CachedSessionUser ?? "", UserSerializationSettings) ?? Users.Guest;

                int SessionUserId = this.Session.Get<int>("LoggedInUserId");

                if (toReturn._Id == 0 && SessionUserId != 0)
                {
                    this.CachedSessionUser = UserCache[SessionUserId];

                    toReturn = JsonConvert.DeserializeObject<User>(this.CachedSessionUser, UserSerializationSettings);

                    if (toReturn._Id == 0)
                    {
                        toReturn = Users.Guest;
                    }
                }

                return toReturn;
            }

            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value), "Log the user out by passing in a new user, or guest");
                }
                if (value._Id != 0)
                {
                    this.LogIn(value);
                }
                else
                {
                    this.LogOut();
                }
            }
        }

        IUser IUserSession.LoggedInUser => this.LoggedInUser;

        public UserSession(ISession session)
        {
            this.Session = session;
        }

        public static void UpdateUser(Updated<User> target)
        {
            if (target is null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (UserCache.ContainsKey(target.Target._Id))
            {
                UserCache[target.Target._Id] = Serialize(target.Target);
            }
        }

        public void AcceptMessage(Updated<User> target)
        {
            UpdateUser(target);
        }

        private static JsonSerializerSettings UserSerializationSettings => new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        private static string Serialize(User u)
        {
            return JsonConvert.SerializeObject(u, UserSerializationSettings);
        }
        protected void LogIn(User u)
        {
            if (u is null)
            {
                throw new ArgumentNullException(nameof(u));
            }

            string SerializedUser = Serialize(u);
            this.CachedSessionUser = SerializedUser;
            UserCache.AddOrUpdate(u._Id, SerializedUser);
            this.Session.Set("LoggedInUserId", u._Id);
        }

        protected void LogOut()
        {
            UserCache.TryRemove(JsonConvert.DeserializeObject<User>(this.CachedSessionUser ?? "", UserSerializationSettings)?._Id ?? 0, out string _);
            this.CachedSessionUser = null;
            this.Session.Set("LoggedInUserId", 0);
        }
    }
}