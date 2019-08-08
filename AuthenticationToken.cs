using Penguin.Entities;
using System;

namespace Penguin.Cms.Web.Security
{
    /// <summary>
    /// A token intended to serve as a temporary login for API access, or password resets
    /// </summary>
    public class AuthenticationToken : Entity
    {
        /// <summary>
        /// When this token will expire
        /// </summary>
        public DateTime Expiration { get; set; }

        /// <summary>
        /// Guid representing the user this token is tied to
        /// </summary>
        public Guid User { get; set; }
    }
}