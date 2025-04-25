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
using Api14.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Api14.Controllers;

[Route("api/[controller]")]
[ApiController]

public class UserController : ControllerBase
{
    private readonly DataContext _context;
    private readonly IJWTService _jwtService;

    public UserController(DataContext context, IJWTService jWTService)
    {
        _context = context;
        _jwtService = jWTService;
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

    ///////////////////////////////////////////////////////
    [HttpGet("get-profile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

    [HttpGet("get-all-users")]
    public ActionResult getUsers()
    {
        var getAll = _context.Users;
        return Ok(getAll);
    }

    [HttpDelete("delete-user/{id}")]
    public ActionResult DeleteUser(int id)
    {
        var usertodelete = _context.Users.FirstOrDefault(u => u.Id == id);
        _context.Users.Remove(usertodelete);
        _context.SaveChanges();

        return Ok(usertodelete);

    }

    ////////////////////////////////////////////////////////////
    [HttpPost("get-reset-code")]
    public ActionResult GetResetCode(string userEmail)
    {

        var user = _context.Users.FirstOrDefault(x => x.Email == userEmail);
        if (user == null)
        {
            var response = new ApiResponse<bool>
            {
                Data = false,
                Message = "user not found",
                Status = StatusCodes.Status404NotFound,
            };
            return NotFound(response);
        }
        else
        {
            if (user.Status == ACCOUNT_STATUS.VERIFIED)
            {
                Random rand = new Random();
                string randomCode = rand.Next(1000, 9999).ToString();

                user.PasswordResetCode = randomCode;

                SMTPService smtpService = new SMTPService();

                smtpService.SendEmail(user.Email, "reset code", $"<p>{user.PasswordResetCode}</p>");

                _context.SaveChanges();

                var response = new ApiResponse<string>
                {
                    Data = "code sent succesfully",
                    Status = StatusCodes.Status200OK,
                    Message = null
                };
                return Ok(response);
            }
            else
            {
                var response = new ApiResponse<bool>
                {
                    Data = false,
                    Status = StatusCodes.Status400BadRequest,
                    Message = " user not verified"
                };
                return BadRequest(response);
            }

        }


    }
    [HttpPut("reset-password")]
    public ActionResult ResetPassword(string email, string code, string newPassword)
    {
        var user = _context.Users.FirstOrDefault(x => x.Email == email);

        if (user == null)
        {
            var response = new ApiResponse<bool>
            {
                Data = false,
                Message = "user not found",
                Status = StatusCodes.Status404NotFound,
            };
            return NotFound(response);
        }
        else
        {
            if (user.PasswordResetCode == code)
            {
                var newPasswordhash = BCrypt.Net.BCrypt.HashPassword(newPassword);

                user.Password = newPasswordhash;
                _context.SaveChanges();

                var response = new ApiResponse<string>
                {
                    Data = "password reset successfully",
                    Status = StatusCodes.Status200OK,
                    Message = null
                };
                return Ok(response);


            }
            else
            {
                var response = new ApiResponse<string>
                {
                    Data = "incorrect code",
                    Status = StatusCodes.Status400BadRequest,
                    Message = null
                };
                return BadRequest(response);
            }
        }
    }

    ////////////////////////////////////////////////////////

    [HttpPost("login")]
    public ActionResult Login(string email, string password)
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
            if (BCrypt.Net.BCrypt.Verify(password, user.Password) && user.Status == ACCOUNT_STATUS.VERIFIED)
            {
                var response = new ApiResponse<UserToken>
                {
                    Data = _jwtService.GetUserToken(user),
                    Status = 200,
                    Message = ""
                };

                return Ok(response);
            }
            else
            {
                var response = new ApiResponse<bool>
                {
                    Data = false,
                    Status = StatusCodes.Status401Unauthorized,
                    Message = "Something went wrong",
                };

                return Unauthorized(response);
            }

        }
    }

}