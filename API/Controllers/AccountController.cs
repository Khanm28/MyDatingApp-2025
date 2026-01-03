using System;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

public class AccountController(AppDbContext context,ITokenService tokenService): BaseApiController
{
    [HttpPost("register")] //api/account/register

    public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
    {
        if(await EmailExist(registerDto.Email)) return BadRequest ("Email Taken");
       using var hmac = new HMACSHA3_512();

       var user= new AppUser
       {
           DisplayName=registerDto.DisplayName,
           Email= registerDto.Email,
           PasswordHash= hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
           PasswordSalt= hmac.Key
       };

       context .Users.Add(user);
       await context.SaveChangesAsync();

       return  user.ToDto(tokenService);
    }

   [HttpPost("login")]
  public async Task<ActionResult<UserDto>> Login (LoginDto loginDto)
    {
        var user = await context.Users.SingleOrDefaultAsync(x=>x.Email==loginDto.Email);
        if(user==null) return Unauthorized("Invalid Email Address");
        using var hmac = new HMACSHA3_512(user.PasswordSalt);
        var ComputedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        for (var i = 0; i < ComputedHash.Length; i++)
        {
           if(ComputedHash[i] !=user.PasswordHash[i])  return Unauthorized("Invalid Password");
        }

       return  user.ToDto(tokenService);

    }
 
    private async Task<bool> EmailExist (string email)
    {
        return await context.Users.AnyAsync(x=>x.Email.ToLower()==email.ToLower());
    }

}


