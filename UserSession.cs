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
        private static readonly ConcurrentDictionary<int, string> UserCache = new();
        private readonly ISession Session;
        private string CachedSessionUser;
        public bool AllowNSFW { get => Session?.Get<bool>("AllowNsfw") ?? false; set => Session?.Set("AllowNsfw", value); }

        public bool IsLocalConnection { get; set; }
        public bool IsLoggedIn => Session != null && Session.Get<int>("LoggedInUserId") != 0;

        public User LoggedInUser
        {
            get
            {
                User toReturn = JsonConvert.DeserializeObject<User>(CachedSessionUser ?? "", UserSerializationSettings) ?? Users.Guest;

                int SessionUserId = Session.Get<int>("LoggedInUserId");

                if (toReturn._Id == 0 && SessionUserId != 0)
                {
                    CachedSessionUser = UserCache[SessionUserId];

                    toReturn = JsonConvert.DeserializeObject<User>(CachedSessionUser, UserSerializationSettings);

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
                    LogIn(value);
                }
                else
                {
                    LogOut();
                }
            }
        }

        IUser IUserSession.LoggedInUser => LoggedInUser;

        public UserSession(ISession session)
        {
            Session = session;
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

        public void AcceptMessage(Updated<User> message)
        {
            UpdateUser(message);
        }

        private static JsonSerializerSettings UserSerializationSettings => new()
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
            CachedSessionUser = SerializedUser;
            _ = UserCache.AddOrUpdate(u._Id, SerializedUser);
            Session.Set("LoggedInUserId", u._Id);
        }

        protected void LogOut()
        {
            _ = UserCache.TryRemove(JsonConvert.DeserializeObject<User>(CachedSessionUser ?? "", UserSerializationSettings)?._Id ?? 0, out _);
            CachedSessionUser = null;
            Session.Set("LoggedInUserId", 0);
        }
    }
}