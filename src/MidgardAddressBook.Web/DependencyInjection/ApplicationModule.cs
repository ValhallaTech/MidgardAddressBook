using System;
using System.Reflection;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using Autofac;
using MidgardAddressBook.BLL.Mapping;
using MidgardAddressBook.BLL.Services;
using MidgardAddressBook.Core.Interfaces;
using MidgardAddressBook.DAL.Caching;
using MidgardAddressBook.DAL.Repositories;

namespace MidgardAddressBook.Web.DependencyInjection;

/// <summary>
/// Registers the BLL, DAL, and AutoMapper components into the Autofac container.
/// </summary>
public class ApplicationModule : Autofac.Module
{
    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Repositories
        builder.RegisterType<AddressBookEntryRepository>()
            .As<IAddressBookEntryRepository>()
            .InstancePerLifetimeScope();

        // Caching
        builder.RegisterType<RedisCacheService>()
            .As<ICacheService>()
            .SingleInstance();

        // Services
        builder.RegisterType<AddressBookService>()
            .As<IAddressBookService>()
            .InstancePerLifetimeScope();

        // AutoMapper profiles (BLL assembly)
        builder.RegisterAutoMapper(typeof(AddressBookEntryProfile).Assembly);
    }
}
