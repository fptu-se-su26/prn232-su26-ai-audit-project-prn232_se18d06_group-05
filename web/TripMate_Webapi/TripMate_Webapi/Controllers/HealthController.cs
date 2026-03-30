using Microsoft.AspNetCore.Mvc;
using Supabase;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly Client _supabase;
    private readonly IConfiguration _config;

    public HealthController(Client supabase, IConfiguration config)
    {
        _supabase = supabase;
        _config = config;
    }

    /// <summary>
    /// Kiểm tra API còn sống không
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow,
            message = "TripMate API is running"
        });
    }

    /// <summary>
    /// Kiểm tra kết nối Supabase
    /// </summary>
    [HttpGet("supabase")]
    public async Task<IActionResult> CheckSupabase()
    {
        try
        {
            // Thử query một bảng đơn giản — nếu bảng chưa có thì vẫn trả về
            // response hợp lệ (empty list), không throw exception
            var result = await _supabase
                .From<SupabasePingModel>()
                .Limit(1)
                .Get();

            return Ok(new
            {
                status = "connected",
                supabaseUrl = _config["Supabase:Url"],
                message = "Supabase connection successful"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "error",
                message = ex.Message
            });
        }
    }
}

// Model tạm để ping — map sang bảng không tồn tại cũng không sao
[Postgrest.Attributes.Table("_ping")]
public class SupabasePingModel : Postgrest.Models.BaseModel { }
