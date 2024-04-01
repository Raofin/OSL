﻿using ErrorOr;
using TopicTalks.Application.Dtos;
using TopicTalks.Application.Dtos.Extensions;
using TopicTalks.Application.Interfaces;
using TopicTalks.Contracts.Common;
using TopicTalks.Domain;
using TopicTalks.Domain.Entities;

namespace TopicTalks.Application.Services;

internal class UserService(IUnitOfWork unitOfWork, IPasswordService passwordService, IAuthService tokenService) : IUserService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IAuthService _tokenService = tokenService;

    public async Task<bool> IsEmailExists(string email)
    {
        return await _unitOfWork.User.IsEmailExists(email);
    }

    public async Task<bool> IsUserExists(long userId)
    {
        return await _unitOfWork.User.IsUserExists(userId);
    }

    public async Task<ErrorOr<User?>> GetAsync(long userId)
    {
        var user = await _unitOfWork.User.GetAsync(userId);

        return user == null
            ? Error.NotFound()
            : user;
    }

    public async Task<ErrorOr<RegistrationResponse>> Register(RegistrationRequest request)
    {
        if (await IsEmailExists(request.Email))
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
            user.UserDetails.Add(new UserDetail {
                Name = request.UserDetails.Name,
                InstituteName = request.UserDetails.InstituteName,
                IdCardNumber = request.UserDetails.IdCardNumber,
            });
        }

        await _unitOfWork.User.AddAsync(user);
        await _unitOfWork.CommitAsync();

        var response = new RegistrationResponse(
            UserId: user.UserId,
            Email: user.Email,
            UserDetails: user.UserDetails.FirstOrDefault().ToDto(),
            Role: user.UserRoles.Select(ur => (RoleName?)ur.RoleId).ToList()
        );

        return response;
    }

    public async Task<ErrorOr<LoginResponse>> Login(LoginRequest request)
    {
        var user = await _unitOfWork.User.GetAsync(request.Email, (long)request.Role);

        if (user == null)
        {
            return Error.NotFound();
        }

        var isUserVerified = _passwordService.VerifyPassword(user.PasswordHash, user.Salt, request.Password);

        if (!isUserVerified)
        {
            return Error.Unauthorized();
        }

        var response = new LoginResponse(
            UserId: user.UserId,
            Token: _tokenService.GenerateJwtToken(user),
            Email: user.Email,
            UserDetails: user.UserDetails.FirstOrDefault().ToDto(),
            Role: user.UserRoles.Select(ur => (RoleName?)ur.RoleId).ToList()
        );

        return response;
    }
}
