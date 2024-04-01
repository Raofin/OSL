﻿using Microsoft.AspNetCore.Mvc;
using TopicTalks.Web.Extensions;
using TopicTalks.Web.Services;
using TopicTalks.Web.ViewModels;

namespace TopicTalks.Web.Controllers;

[Route("answer")]
public class AnswerController(IHttpService httpService) : Controller
{
    private readonly IHttpService _httpService = httpService;

    [HttpPost]
    public async Task<IActionResult> PostAnswer(AnswerRequestViewModel request)
    {
        var response = await _httpService.Client.PostAsync("api/answer", request.ToStringContent());
        var x = response.DeserializeTo<AnswerViewModel>();
        return response.IsSuccessStatusCode
            ? Ok(response.DeserializeTo<AnswerViewModel>())
            : new StatusCodeResult((int)response.StatusCode);
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateAnswer(AnswerUpdateRequestViewModel request)
    {
        var response = await _httpService.Client.PatchAsync("api/answer", request.ToStringContent());

        return response.IsSuccessStatusCode
            ? Ok(response.DeserializeTo<AnswerViewModel>())
            : new StatusCodeResult((int)response.StatusCode);
    }

    [HttpDelete("{answerId}")]
    public async Task<IActionResult> DeleteAnswer(long answerId)
    {
        var response = await _httpService.Client.DeleteAsync($"api/answer/{answerId}");

        return response.IsSuccessStatusCode
            ? Ok()
            : new StatusCodeResult((int)response.StatusCode);
    }

    [HttpGet("{answerId}")]
    public async Task<IActionResult> GetAnswer(long answerId)
    {
        var response = await _httpService.Client.GetAsync($"api/answer/{answerId}");
        var res = response.ToJson();

        var x = response.DeserializeTo<AnswerViewModel>();
        return response.IsSuccessStatusCode
            ? Ok(x)
            : new StatusCodeResult((int)response.StatusCode);
    }
}