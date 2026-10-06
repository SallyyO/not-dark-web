using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("[controller]")]
public class SWAGController : ControllerBase
{
    [HttpGet]
    public string Get()
    {
        return "SWAGGING";
    }
}