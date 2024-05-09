using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;


namespace APICatalogo.Controllers;

[Route("api/teste")]
[ApiController]
[ApiVersion("2.0")]
public class TesteV2Controller : ControllerBase
{
    public string GetVersion() 
    {
        return "TesteV2 - GET - Api Versão 2.0";
    }
}
