using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Cms.Web.Security.Interfaces
{
    /// <summary>
    /// Represents a security validation attempt
    /// </summary>
    public interface ISecurityToken
    {
        /// <summary>
        /// If true, access should be allowed
        /// </summary>
        bool IsValid { get; }
    }
}