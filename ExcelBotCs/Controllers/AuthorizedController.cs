using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExcelBotCs.Controllers;

[Authorize(AuthenticationSchemes =
    CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme)]
public abstract class AuthorizedController : ControllerBase
{
    protected readonly ILogger Logger;

    protected AuthorizedController(ILogger logger)
    {
        Logger = logger;
    }
}