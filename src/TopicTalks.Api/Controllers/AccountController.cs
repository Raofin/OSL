﻿using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TopicTalks.Api.Attributes;
using TopicTalks.Application.Dtos;
using TopicTalks.Application.Extensions;
using TopicTalks.Application.Interfaces;

namespace TopicTalks.Api.Controllers;

public class AccountController(IUserService userService) : ApiController
{
    private readonly IUserService _userService = userService;

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegistrationRequest request)
    {
        var registration = await _userService.Register(request);

        return registration.IsError switch 
        {
            false => Ok(registration.Value),
            _ => registration.Errors.Any(e => e.Type is ErrorType.Conflict)
                ? Conflict("User with the provided email already exists.")
                : Problem("An unexpected error occurred.")
        };
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var login = await _userService.Login(request);

        return login.IsError switch 
        {
            false => Ok(login.Value),
            _ => login.Errors.Any(e => e.Type is ErrorType.NotFound or ErrorType.Unauthorized)
                ? Unauthorized("Invalid Credentials.")
                : Problem("An unexpected error occurred.")
        };
    }

    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword(PasswordChangeRequest request)
    {
        var passwordChange = await _userService.ChangePassword(User.GetUserId(), request);

        return passwordChange.IsError switch {
            false => Ok("Password was successfully updated."),
            _ => passwordChange.Errors.Any(e => e.Type is ErrorType.Unauthorized)
                ? Unauthorized("Invalid old password.")
                : Problem("An unexpected error occurred.")
        };
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetUser()
    {
        var user = await _userService.GetWithDetailsAsync(User.GetUserId());

        return !user.IsError
            ? Ok(user.Value)
            : user.FirstError.Type switch {
                ErrorType.NotFound => NotFound("User was not found."),
                _ => Problem("An unexpected error occurred.")
            };
    }

    [AuthorizeModerator]
    [HttpGet("excel/users")]
    public async Task<IActionResult> GetExcel()
    {
        var excelFile = await _userService.UserListExcelFile();

        return File(excelFile.Bytes, excelFile.ContentType, excelFile.Name);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyOtp(VerifyRequest? verify)
    {
        if (verify is null)
        {
            await _userService.SendOtp(User.GetEmail());

            return Ok("Otp was sent successfully.");
        }

        var verification = await _userService.VerifyOtp(User.GetEmail(), verify.Code);

        return verification.IsError 
            ? BadRequest("Invalid Otp.") 
            : Ok(verification.Value);
    }
}
