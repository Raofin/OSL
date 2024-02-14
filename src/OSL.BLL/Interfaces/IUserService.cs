﻿using ErrorOr;
using OSL.BLL.Models;
using OSL.DAL.Entities;

namespace OSL.BLL.Interfaces;

public interface IUserService
{
    Task<ErrorOr<User>> Get(long? userId);
    Task<bool> IsEmailExists(string email);
    Task<ErrorOr<User>> Login(LoginVM model);
    Task<ErrorOr<User>> RegisterUser(RegisterVM model);
}