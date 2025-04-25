using Api14.CORE;
using Api14.Models;

namespace Api14.Services.Interfaces
{
    public interface IJWTService
    {
        UserToken GetUserToken(User user);
    }
}
