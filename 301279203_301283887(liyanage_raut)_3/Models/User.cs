using System;
using System.Collections.Generic;

namespace _301279203_301283887_liyanage_raut__3.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string UserPassword { get; set; } = null!;
}
