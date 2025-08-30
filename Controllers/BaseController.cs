using Microsoft.AspNetCore.Mvc;
using BussinessCupApi.Data;
[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    protected readonly ApplicationDbContext _context;

    public BaseController(ApplicationDbContext context)
    {
        _context = context;
    }
} 