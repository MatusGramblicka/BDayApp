﻿using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace BDayServer.MigrationManager
{
    public static class MigrationManager
    {
        public static IHost MigrateDatabase(this IHost host)
        {
            using var scope = host.Services.CreateScope();
            using var appContext = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
            try
            {
                appContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                //Log errors or do anything you think it's needed
                throw;
            }

            return host;
        }
    }
}
