﻿using Microsoft.AspNetCore.Identity;

namespace Entities;

public class User : IdentityUser
{
    public string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public bool IsAdmin { get; set; }
}