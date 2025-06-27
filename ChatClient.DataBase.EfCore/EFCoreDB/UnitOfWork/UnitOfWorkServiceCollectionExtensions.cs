// Copyright (c) Arch team. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ChatClient.DataBase.EfCore.EFCoreDB.UnitOfWork
{
    /// <summary>
    /// Extension methods for setting up unit of work related services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class UnitOfWorkServiceCollectionExtensions
    {
        public static IContainerRegistry AddUnitOfWork<TContext>(this IContainerRegistry services)
            where TContext : DbContext
        {
            services.RegisterScoped<IRepositoryFactory, UnitOfWork<TContext>>();
            services.RegisterScoped<IUnitOfWork, UnitOfWork<TContext>>();
            services.RegisterScoped<IUnitOfWork<TContext>, UnitOfWork<TContext>>();

            return services;
        }

        public static IContainerRegistry AddCustomRepository<TEntity, TRepository>(this IContainerRegistry services)
            where TEntity : class
            where TRepository : class, IRepository<TEntity>
        {
            services.RegisterScoped<IRepository<TEntity>, TRepository>();
            return services;
        }
    }
}