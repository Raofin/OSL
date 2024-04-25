﻿using TopicTalks.Application.Dtos;
using TopicTalks.Domain.Entities;
using TopicTalks.Domain.Enums;

namespace TopicTalks.Application.Extensions;

public static class EntityExtensions
{
    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            UserId: user.UserId,
            Username: user.Username,
            Email: user.Email,
            IsVerified: user.IsVerified,
            UserDetails: user.UserDetails.ToDto(),
            Roles: user.UserRoles.Select(ur => (RoleType)ur.RoleId).ToList(),
            CreatedAt: user.CreatedAt,
            ImageFileId: user.ImageFileId
        );
    }

    public static UserBasicInfoDto ToBasicInfoDto(this User user)
    {
        return new UserBasicInfoDto(
            UserId: user.UserId,
            Username: user.Username,
            Email: user.Email
        );
    }

    public static QuestionResponseDto ToDto(this Question question)
    {
        return new QuestionResponseDto(
            QuestionId: question.QuestionId,
            Topic: question.Topic,
            Explanation: question.Explanation,
            UserInfo: question.User?.ToBasicInfoDto(),
            CreatedAt: question.CreatedAt,
            UpdatedAt: question.UpdatedAt
        );
    }

    public static UserDetailDto? ToDto(this UserDetail? userDetail)
    {
        return userDetail == null ? null
            : new UserDetailDto(
                Name: userDetail.FullName,
                InstituteName: userDetail.InstituteName,
                IdCardNumber: userDetail.IdCardNumber
            );
    }

    public static AnswerResponseDto ToDto(this Answer answer)
    {
        return new AnswerResponseDto(
            AnswerId: answer.AnswerId,
            ParentAnswerId: answer.ParentAnswerId,
            QuestionId: answer.QuestionId,
            Explanation: answer.Explanation,
            CreatedAt: answer.CreatedAt,
            UserInfo: answer.User?.ToBasicInfoDto()
        );
    }
}