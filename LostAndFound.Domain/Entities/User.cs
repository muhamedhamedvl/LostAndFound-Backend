using System;
using System.Collections.Generic;

namespace LostAndFound.Domain.Entities
{
    // Legacy alias kept for backward compatibility while transitioning to AppUser/Identity.
    // This lets existing code continue to use `User` while the underlying table and relationships
    // are provided by the Identity-based AppUser.
    public class User : AppUser
    {
    }
}
