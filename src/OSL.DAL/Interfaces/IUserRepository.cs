﻿using ErrorOr;
using OSL.DAL.Entities;

namespace OSL.DAL.Interfaces;

public interface IUserRepository
{
    Task<bool> IsEmailExists(string email);
    Task<bool> IsUserExists(long? userId);
    Task<ErrorOr<User>> Get(string email, long roleId);
    Task<ErrorOr<User>> Get(long? userId);
    Task<ErrorOr<User>> Register(User user, UserRole userRole, UserDetail? userDetail);
}