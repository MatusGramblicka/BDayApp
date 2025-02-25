﻿using BDayClient.HttpInterceptor;
using BDayClient.HttpRepository;
using Entities.Models;
using Entities.RequestFeatures;
using Microsoft.AspNetCore.Components;

namespace BDayClient.Pages;

public partial class Index
{
    public List<Person> PersonList { get; set; } = new();
    public MetaData MetaData { get; set; } = new();

    private PersonParameters _personParameters = new();

    [Inject]
    public IPersonHttpRepository PersonRepo { get; set; }

    [Inject]
    public HttpInterceptorService Interceptor { get; set; }

    protected override async Task OnInitializedAsync()
    {
        Interceptor.RegisterEvent();
        Interceptor.RegisterBeforeSendEvent();

        await GetPersons();
    }

    private async Task GetPersons()
    {
        _personParameters.PageSize = 50;
        var pagingResponse = await PersonRepo.GetPersons(_personParameters);

        PersonList = pagingResponse.Items;
        MetaData = pagingResponse.MetaData;
    }

    public void Dispose() => Interceptor.DisposeEvent();
}