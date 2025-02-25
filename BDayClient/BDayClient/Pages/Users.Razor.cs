﻿using BDayClient.Features;
using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using Blazored.LocalStorage;
using Contracts.DataTransferObjects.User;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace BDayClient.Pages;

public partial class Users : IDisposable
{
    public List<UserLite> UsersList { get; set; } = new();
    public string LoggedUserName { get; set; }

    [Inject] public IUsersHttpRepository UsersHttpRepository { get; set; }
    [Inject] public HttpInterceptorService Interceptor { get; set; }
    [Inject] public NavigationManager NavigationManager { get; set; }
    [Inject] public ILocalStorageService LocalStorageService { get; set; }
    [Inject] public ILogger<Users> Logger { get; set; }

    private bool _alreadyDisposed;

    protected override async Task OnInitializedAsync()
    {
        Interceptor.RegisterEvent();
        Interceptor.RegisterBeforeSendEvent();

        await GetUsers();

        var token = await LocalStorageService.GetItemAsync<string>("authToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            LoggedUserName = JwtParser.GetLoggedUserName(token);
            Logger.LogInformation(LoggedUserName);
        }
    }

    private async Task GetUsers()
    {
        UsersList = await UsersHttpRepository.GetUsers();

        Logger.LogInformation(JsonConvert.SerializeObject(UsersList));
    }

    private async Task UpdateUser(UserLite user)
    {
        await UsersHttpRepository.UpdateUser(user);
        await GetUsers();
    }

    private async Task RemoveAdminRole(UserLite user)
    {
        await UsersHttpRepository.RemoveAdminRole(user);
        await GetUsers();
    }

    private async Task DeleteUser(UserLite user)
    {
        await UsersHttpRepository.DeleteUser(user);
        await GetUsers();
    }

    private async Task Change2StepsAuthorization(UserLite2StepsAuthDto user2StepsAuth)
    {
        await UsersHttpRepository.SetTwoFactorAuthorization(user2StepsAuth);
        await GetUsers();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_alreadyDisposed)
            return;

        if (disposing)
        {
            Interceptor.DisposeEvent();
            _alreadyDisposed = true;
        }
    }

    ~Users()
    {
        Dispose(disposing: false);
    }
}