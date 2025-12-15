using Microsoft.AspNetCore.Mvc;

namespace AuxiliumAPI.Controllers
{
    [ApiController]
    [Route("/api/v3/cases/{case_id}/todos")]
    [Tags("Cases")]
    public class CaseTodoController : ControllerBase
    {
    }
}
