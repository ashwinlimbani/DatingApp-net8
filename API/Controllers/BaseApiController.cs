using API.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ServiceFilter(typeof(LogUserActivity))]
[Route("api/[controller]")] // /api/users
[ApiController]
public class BaseApiController : ControllerBase
{

}
