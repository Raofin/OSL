﻿using ErrorOr;
using TopicTalks.Application.Interfaces;
using TopicTalks.Domain.Entities;
using TopicTalks.Domain.Interfaces;
using TopicTalks.Domain.Models;

namespace TopicTalks.Application.Services;

internal class QuestionService(IQuestionRepository _questionRepository) : IQuestionService
{
    public async Task<ErrorOr<Question>> CreateQuestion(QuestionVM model)
    {
        try
        {
            var question = new Question {
                Topic = model.Topic,
                Explanation = model.Explanation,
                UserId = model.UserId
            };

            return await _questionRepository.CreateQuestion(question);
        }
        catch (Exception ex)
        {
            return Error.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Question>> Get(long questionId)
    {
        try
        {
            return await _questionRepository.Get(questionId);
        }
        catch (Exception ex)
        {
            return Error.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<QuestionModel>>> Get(string? searchText)
    {
        try
        {
            return await _questionRepository.Get(searchText);
        }
        catch (Exception ex)
        {
            return Error.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Question>>> GetMyQuestions(long userId)
    {
        try
        {
            return await _questionRepository.GetMyQuestions(userId);
        }
        catch (Exception ex)
        {
            return Error.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<List<Question>>> GetMyRespondedQuestions(long userId)
    {
        try
        {
            return await _questionRepository.GetMyRespondedQuestions(userId);
        }
        catch (Exception ex)
        {
            return Error.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Question>> UpdateQuestion(QuestionVM model)
    {
        try
        {
            var question = new Question {
                QuestionId = model.QuestionId,
                Topic = model.Topic,
                Explanation = model.Explanation,
                UserId = model.UserId
            };

            return await _questionRepository.Update(question);
        }
        catch (Exception ex)
        {
            return Error.Failure($"Error: {ex.Message}");
        }
    }

    public async Task<ErrorOr<long>> DeleteQuestion(long questionId)
    {
        try
        {
            return await _questionRepository.Delete(questionId);
        }
        catch (Exception ex)
        {
            return Error.Failure($"Error: {ex.Message}");
        }
    }
}