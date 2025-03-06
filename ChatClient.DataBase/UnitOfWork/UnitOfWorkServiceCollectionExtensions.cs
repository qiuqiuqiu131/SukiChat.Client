// Copyright (c) Arch team. All rights reserved.

using ChatClient.DataBase.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;

namespace ChatServer.DataBase.UnitOfWork
{
    /// <summary>
    /// Extension methods for setting up unit of work related services in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class UnitOfWorkServiceCollectionExtensions
    {
        public static IContainerRegistry AddUnitOfWork<TContext>(this IContainerRegistry services) where TContext : DbContext
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
