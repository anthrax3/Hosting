// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Builder.Internal;
using Microsoft.AspNet.Hosting.Fakes;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Xunit;

namespace Microsoft.AspNet.Hosting.Tests
{
    public class StartupManagerTests : IFakeStartupCallback
    {
        private readonly IList<object> _configurationMethodCalledList = new List<object>();

        [Fact]
        public void StartupClassMayHaveHostingServicesInjected()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = "WithServices" };
            var loader = new StartupLoader(services, hostingEnv);
            var type = loader.FindStartupType("Microsoft.AspNet.Hosting.Tests", diagnosticMessages);
            var startup = loader.LoadMethods(type, diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            Assert.Equal(2, _configurationMethodCalledList.Count);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Dev")]
        [InlineData("Retail")]
        [InlineData("Static")]
        [InlineData("StaticProvider")]
        [InlineData("Provider")]
        [InlineData("ProviderArgs")]
        [InlineData("BaseClass")]
        public void StartupClassAddsConfigureServicesToApplicationServices(string environment)
        {
            var services = new ServiceCollection().BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = environment };
            var loader = new StartupLoader(services, hostingEnv);
            var type = loader.FindStartupType("Microsoft.AspNet.Hosting.Tests", diagnosticMessages);
            var startup = loader.LoadMethods(type, diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(new ServiceCollection());
            startup.ConfigureDelegate(app);

            var options = app.ApplicationServices.GetRequiredService<IOptions<FakeOptions>>().Value;
            Assert.NotNull(options);
            Assert.True(options.Configured);
            Assert.Equal(environment, options.Environment);
        }

        [Fact]
        public void StartupWithNoConfigureThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = "Boom" };
            var loader = new StartupLoader(services, hostingEnv);
            var type = loader.FindStartupType("Microsoft.AspNet.Hosting.Tests", diagnosticMessages);

            var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadMethods(type, diagnosticMessages));
            Assert.Equal("A method named 'ConfigureBoom' or 'Configure' in the type 'Microsoft.AspNet.Hosting.Fakes.StartupBoom' could not be found.", ex.Message);
        }

        [Fact]
        public void StartupWithTwoConfiguresThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = "TwoConfigures" };
            var loader = new StartupLoader(services, hostingEnv);
            var type = loader.FindStartupType("Microsoft.AspNet.Hosting.Tests", diagnosticMessages);

            var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadMethods(type, diagnosticMessages));
            Assert.Equal("Having multiple overloads of method 'Configure' is not supported.", ex.Message);
        }

        [Fact]
        public void StartupWithTwoConfigureServicesThrows()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddInstance<IFakeStartupCallback>(this);
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = "TwoConfigureServices" };
            var loader = new StartupLoader(services, hostingEnv);
            var type = loader.FindStartupType("Microsoft.AspNet.Hosting.Tests", diagnosticMessages);

            var ex = Assert.Throws<InvalidOperationException>(() => loader.LoadMethods(type, diagnosticMessages));
            Assert.Equal("Having multiple overloads of method 'ConfigureServices' is not supported.", ex.Message);
        }

        [Fact]
        public void StartupClassCanHandleConfigureServicesThatReturnsNull()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = "WithNullConfigureServices" };
            var loader = new StartupLoader(services, hostingEnv);
            var type = loader.FindStartupType("Microsoft.AspNet.Hosting.Tests", diagnosticMessages);
            var startup = loader.LoadMethods(type, diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(new ServiceCollection());
            Assert.NotNull(app.ApplicationServices);
            startup.ConfigureDelegate(app);
            Assert.NotNull(app.ApplicationServices);
        }

        [Fact]
        public void StartupClassWithConfigureServicesShouldMakeServiceAvailableInConfigure()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = "WithConfigureServices" };
            var loader = new StartupLoader(services, hostingEnv);
            var type = loader.FindStartupType("Microsoft.AspNet.Hosting.Tests", diagnosticMessages);
            var startup = loader.LoadMethods(type, diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            var foo = app.ApplicationServices.GetRequiredService<StartupWithConfigureServices.IFoo>();
            Assert.True(foo.Invoked);
        }

        [Fact]
        public void StartupLoaderCanLoadByType()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment();
            var loader = new StartupLoader(services, hostingEnv);
            var startup = loader.LoadMethods(typeof(TestStartup), diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);
            startup.ConfigureDelegate(app);

            var foo = app.ApplicationServices.GetRequiredService<SimpleService>();
            Assert.Equal("Configure", foo.Message);
        }

        [Fact]
        public void StartupLoaderCanLoadByTypeWithEnvironment()
        {
            var serviceCollection = new ServiceCollection();
            var services = serviceCollection.BuildServiceProvider();

            var diagnosticMessages = new List<string>();
            var hostingEnv = new HostingEnvironment { EnvironmentName = "No" };
            var loader = new StartupLoader(services, hostingEnv);
            var startup = loader.LoadMethods(typeof(TestStartup), diagnosticMessages);

            var app = new ApplicationBuilder(services);
            app.ApplicationServices = startup.ConfigureServicesDelegate(serviceCollection);

            var ex = Assert.Throws<TargetInvocationException>(() => startup.ConfigureDelegate(app));
            Assert.IsAssignableFrom(typeof(InvalidOperationException), ex.InnerException);
        }

        public class SimpleService
        {
            public SimpleService()
            {
            }

            public string Message { get; set; }
        }

        public class TestStartup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddSingleton<SimpleService>();
            }

            public void ConfigureNoServices(IServiceCollection services)
            {
            }

            public void Configure(IApplicationBuilder app)
            {
                var service = app.ApplicationServices.GetRequiredService<SimpleService>();
                service.Message = "Configure";
            }

            public void ConfigureNo(IApplicationBuilder app)
            {
                var service = app.ApplicationServices.GetRequiredService<SimpleService>();
            }
        }

        public void ConfigurationMethodCalled(object instance)
        {
            _configurationMethodCalledList.Add(instance);
        }
    }
}
