using Penguin.Cms.Entities;

namespace Penguin.Cms.Web.Security
{
    /// <summary>
    /// Used to track whether or not a user has validated their email
    /// </summary>
    public class EmailValidationToken : UserAuditableEntity
    {
        /// <summary>
        /// True if the user has validated this token by clicking the link in their email
        /// </summary>
        public bool IsValidated { get; set; }
    }
}