using Api14.Data;
using Api14.Models;
using Api14.Requests;
using Api14.SMTP;
using Api14.CORE;
using BCrypt;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Api14.Enums;

namespace Api14.Controllers;

[Route("api/[controller]")]
[ApiController]

public class UserController : ControllerBase
{
    private readonly DataContext _context;

    public UserController(DataContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public ActionResult Register(AddUser request)
    {
        var userExists = _context.Users.FirstOrDefault(u => u.Email == request.Email);

        if (userExists == null)
        {
            var user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
            };
            user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);

            Random rand = new Random();
            string randomCode = rand.Next(10000, 99999).ToString();

            user.VerificationCode = randomCode;

            SMTPService smtpService = new SMTPService();

            smtpService.SendEmail(user.Email, "Verification", $"<p>{user.VerificationCode}</p>");


            _context.Users.Add(user);
            _context.SaveChanges();

            var response = new ApiResponse<User>
            {
                Data = user,
                Status = StatusCodes.Status200OK,
                Message = null,
            };

            return Ok(response);
        }
        else
        {
            var response = new ApiResponse<bool>
            {
                Data = false,
                Status = StatusCodes.Status409Conflict,
                Message = "User Already Exists",
            };
            
            return BadRequest(response);
        }
    }

    [HttpPost("verify-email/{email}/{code}")]
    public ActionResult Verify(string email, string code)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            var response = new ApiResponse<bool>
            {
                Data = false,
                Message = "User Not Found",
                Status = StatusCodes.Status404NotFound,
            };
            
            return NotFound(response);
        }
        else
        {
            if (user.VerificationCode == code)
            {
                user.Status = ACCOUNT_STATUS.VERIFIED;
                user.VerificationCode = null;
                
                _context.SaveChanges();
                var response = new ApiResponse<bool>
                {
                    Data = true,
                    Message = "User Verified",
                    Status = StatusCodes.Status200OK,
                };
                return Ok(response);
            }
            else
            {
                var response = new ApiResponse<bool>
                {
                    Data = false,
                    Message = "Wrong Verification Code",
                    Status = StatusCodes.Status400BadRequest,
                };
                return BadRequest(response);
            }
        }
    }

    [HttpGet("get-profile")]
    public ActionResult GetProfile(int id)
    {
        var user = _context.Users.FirstOrDefault(x => x.Id == id);

        if (user == null)
        {
            var response = new ApiResponse<bool>
            {
                Data = false,
                Message = "User Not Found",
                Status = StatusCodes.Status404NotFound,
            };
            
            return NotFound(response);
        }
        else
        {
            if (user.Status == ACCOUNT_STATUS.VERIFIED)
            {
                var response = new ApiResponse<User>
                {
                    Data = user,
                    Message = null,
                    Status = StatusCodes.Status200OK,
                };
                
                return Ok(response);
            }
            else
            {
                var response = new ApiResponse<bool>
                {
                    Data = false,
                    Message = "User Not Verified",
                    Status = StatusCodes.Status400BadRequest,
                };
            
                return BadRequest(response);
            }
        }
    }
}