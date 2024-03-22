﻿using ErrorOr;
using TopicTalks.Application.Interfaces;
using TopicTalks.Domain.Entities;
using TopicTalks.Domain.Interfaces;
using TopicTalks.Domain.Models;

namespace TopicTalks.Application.Services;

internal class UserService(IPasswordHashService _passwordHashService, IUserRepository _userRepository, IAuthService _tokenService) : IUserService
{
    public async Task<bool> IsEmailExists(string email)
    {
        return await _userRepository.IsEmailExists(email);
    }

    public async Task<bool> IsUserExists(long userId)
    {
        return await _userRepository.IsUserExists(userId);
    }

    public async Task<ErrorOr<User>> RegisterUser(RegisterVM model)
    {
        try
        {
            if (await IsEmailExists(model.Email))
            {
                return Error.Conflict();
            }

            (string hashedPassword, string salt) = _passwordHashService.HashPasswordWithSalt(model.Password);

            var user = new User {
                Email = model.Email,
                PasswordHash = hashedPassword,
                Salt = salt
            };

            var userRole = new UserRole {
                RoleId = (long)model.Role,
            };

            return await _userRepository.Register(user, userRole, model.UserDetail);
        }
        catch (Exception ex)
        {
            return Error.Unexpected(description: ex.Message);
        }
    }

    public async Task<ErrorOr<string>> Login(LoginVM model)
    {
        try
        {
            var user = await _userRepository.Get(model.Email, (long)model.Role);

            if (user.IsError)
            {
                return user.Errors;
            }

            var isUserVerified = _passwordHashService.VerifyPassword(user.Value.PasswordHash, user.Value.Salt, model.Password);

            if (isUserVerified)
            {
                var token = _tokenService.GenerateJwtToken(user.Value);

                return token;
            }

            return Error.Unauthorized(description: "Invalid crediantials");
        }
        catch (Exception ex)
        {
            return Error.Unexpected(description: ex.Message);
        }
    }

    public async Task<ErrorOr<User>> Get(long? userId)
    {
        return await _userRepository.Get(userId);
    }
}