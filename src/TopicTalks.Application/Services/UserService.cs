﻿using ErrorOr;
using TopicTalks.Application.Dtos;
using TopicTalks.Application.Extensions;
using TopicTalks.Application.Interfaces;
using TopicTalks.Application.Interfaces.Excel;
using TopicTalks.Domain;
using TopicTalks.Domain.Entities;
using TopicTalks.Domain.Enums;

namespace TopicTalks.Application.Services;

internal class UserService(
    IUnitOfWork unitOfWork, 
    IPasswordService passwordService, 
    IAuthService tokenService, 
    IExcelExportService excelExportService,
    IEmailService emailService) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IAuthService _tokenService = tokenService;
    private readonly IExcelExportService _excelExportService = excelExportService;
    private readonly IEmailService _emailService = emailService;

    public async Task<ErrorOr<UserDto>> GetWithDetailsAsync(long userId)
    {
        var user = await _unitOfWork.User.GetWithDetailsAsync(userId);

        if (user is null)
        {
            return Error.NotFound();
        }

        var userDto = new UserDto(
                UserId: user.UserId,
                Email: user.Email,
                IsVerified: user.IsVerified,
                CreatedAt: user.CreatedAt,
                UserDetails: user.UserDetails.ToDto(),
                Roles: user.UserRoles.Select(ur => (RoleType)ur.RoleId).ToList()
            );

        return userDto;
    }

    public async Task<ErrorOr<AuthenticationResponse>> Register(RegistrationRequest request)
    {
        var isEmailExists = await _unitOfWork.User.IsEmailExists(request.Email);

        if (isEmailExists)
        {
            return Error.Conflict();
        }

        var (hashedPassword, salt) = _passwordService.HashPasswordWithSalt(request.Password);

        var user = new User {
            Email = request.Email,
            PasswordHash = hashedPassword,
            Salt = salt,
        };

        user.UserRoles.Add(new UserRole {
            RoleId = (long)request.Role,
        });

        if (request?.UserDetails != null)
        {
            user.UserDetails = new UserDetail {
                Name = request.UserDetails.Name,
                InstituteName = request.UserDetails.InstituteName,
                IdCardNumber = request.UserDetails.IdCardNumber,
            };
        }

        await _unitOfWork.User.AddAsync(user);
        await _unitOfWork.CommitAsync();
        await _emailService.SendWelcomeAsync(user.Email);

        return Authenticate(user);
    }

    public async Task<ErrorOr<AuthenticationResponse>> Login(LoginRequest request)
    {
        var user = await _unitOfWork.User.GetWithDetailsAsync(request.Email, (long)request.Role);

        if (user == null)
        {
            return Error.NotFound();
        }

        var isUserVerified = _passwordService.VerifyPassword(user.PasswordHash, user.Salt, request.Password);

        if (!isUserVerified)
        {
            return Error.Unauthorized();
        }

        return Authenticate(user);
    }

    private AuthenticationResponse Authenticate(User user)
    {
        return new AuthenticationResponse(
            Token: _tokenService.GenerateJwtToken(user),
            User: new UserDto(
                UserId: user.UserId,
                Email: user.Email,
                IsVerified: user.IsVerified,
                UserDetails: user.UserDetails.ToDto(),
                Roles: user.UserRoles.Select(ur => (RoleType)ur.RoleId).ToList(),
                CreatedAt: user.CreatedAt
            )
        );
    }

    public async Task<ExcelFile> UserListExcelFile()
    {
        var users = await _unitOfWork.User.GetWithDetailsAsync();

        var usersDto = users.Select(u => new UserDto(
                UserId: u.UserId,
                Email: u.Email,
                CreatedAt: u.CreatedAt,
                IsVerified: u.IsVerified,
                UserDetails: u.UserDetails.ToDto(),
                Roles: u.UserRoles.Select(ur => (RoleType)ur.RoleId).ToList()
            )
        ).ToList();

        return _excelExportService.UserListExcel(usersDto);
    }

    public async Task<ErrorOr<Success>> ChangePassword(long userId, PasswordChangeRequest request)
    {
        var user = await _unitOfWork.User.GetAsync(userId);

        if (user is null)
        {
            return Error.Unexpected();
        }

        var isPasswordVerified = _passwordService.VerifyPassword(user.PasswordHash, user.Salt, request.OldPassword);

        if (!isPasswordVerified)
        {
            return Error.Unauthorized();
        }

        var (hashedPassword, salt) = _passwordService.HashPasswordWithSalt(request.NewPassword);

        user.PasswordHash = hashedPassword;
        user.Salt = salt;

        await _unitOfWork.CommitAsync();

        return Result.Success;
    }

    #region ### OTP ###

    public async Task SendOtp(string email)
    {
        var code = new Random().Next(1000, 9999).ToString();

        var otp = await _unitOfWork.Otp.GetOtpAsync(email);

        if (otp is not null)
        {
            _unitOfWork.Otp.Remove(otp);
        }

        await _unitOfWork.Otp.AddAsync(new Otp {
            Email = email,
            Code = code,
            ExpiresAt = DateTime.Now.AddMinutes(5),
        });

        await _unitOfWork.CommitAsync();
        await _emailService.SendOtpAsync(email, code);
    }

    public async Task<ErrorOr<AuthenticationResponse>> VerifyOtp(string email, string code)
    {
        var otp = await _unitOfWork.Otp.GetValidOtpAsync(email, code);

        if (otp is null)
        {
            return Error.Unauthorized();
        }

        _unitOfWork.Otp.Remove(otp);

        var user = await _unitOfWork.User.GetByEmailAsync(email);
        user.IsVerified = true;

        await _unitOfWork.CommitAsync();
        await _emailService.SendVerifiedAsync(email);

        return Authenticate(user);
    }

    #endregion ### OTP ###
}
