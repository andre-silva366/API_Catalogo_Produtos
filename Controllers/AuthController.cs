using APICatalogo.DTOs;
using APICatalogo.Models;
using APICatalogo.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace APICatalogo.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;

    public AuthController(ITokenService tokenService, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        _tokenService = tokenService;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
    }

    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginModelDTO modelDTO)
    {
        var user = await _userManager.FindByNameAsync(modelDTO.UserName!);

        if(user is not null && await _userManager.CheckPasswordAsync(user, modelDTO.Password!)) 
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString(ToString()))



            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = _tokenService.GenerateAccessToken(authClaims, _configuration);
            var refreshToken = _tokenService.GenerateRefreshToken();
            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes);
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddMinutes(refreshTokenValidityInMinutes);
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken, 
                Expiration = token.ValidTo
            });

        }

        return Unauthorized();
        
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModelDTO model)
    {
        // verifica se o nome do usuario existe
        var userExists = await _userManager.FindByNameAsync(model.UserName!);

        if (userExists != null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDTO { Status = "Error", Message = "User already exists!" });
                        
        }

        // caso não exista sera criado
        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.UserName,
        };

        var result = await _userManager.CreateAsync(user, model.Password!);

        if(!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ResponseDTO { Status = "Error", Message = "User creation failed!" });
        }

        return Ok(new ResponseDTO { Status = "Sucess", Message = "User created successfully!" });
    }

    [HttpPost]
    [Route("refresh-token")]
    public async Task<IActionResult> RefreshToken(TokenModelDTO tokenModelDTO)
    {
        // verifica se o token é nulo
        if(tokenModelDTO is null)
        {
            return BadRequest("Invalid client request");
        }

        // verifica se os valores obtidos são do tipo correto
        string ? acessToken = tokenModelDTO.AccessToken ?? throw new ArgumentNullException(nameof(tokenModelDTO));
        string? refreshToken = tokenModelDTO.RefreshToken ?? throw new ArgumentNullException(nameof(tokenModelDTO));

        // gerar uma claim usando o token vencido para gerar um novo token
        var principal = _tokenService.GetPrincipalFromExpiredToken(acessToken!, _configuration);

        if(principal == null)
        {
            return BadRequest("Invalid access token/refresh token");
        }

        string userName = principal.Identity.Name;
        var user = await _userManager.FindByNameAsync(userName!);

        if(user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
        {
            return BadRequest("Invalid access token/refresh token");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims.ToList(), _configuration);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        
        user.RefreshToken = newRefreshToken;
        await _userManager.UpdateAsync(user);

        return new ObjectResult(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
            refreshToken = newRefreshToken
        });
    }

    [Authorize]
    [HttpPost]
    [Route("revoke/{username}")]
    public async Task<IActionResult> Revoke(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if(user == null)
        {
            return BadRequest("Invalid user name");
        }

        user.RefreshToken = null;
        await _userManager.UpdateAsync(user);
        return NoContent();
    }

}
