﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using TxtLauncher.ViewModels;
using TxtLauncher.Views;

namespace TxtLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ProcessUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            //LogHandlerApplication.ProccessException(e.Exception);
            e.Handled = true;
        }

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            DispatcherUnhandledException += ProcessUnhandledException;

            var serviceCollection = new ServiceCollection();
            var startupService = new StartupService();

            startupService.Configure(serviceCollection);
            serviceCollection.BuildServiceProvider();

            var main = new MainViewModel();
            var window = new MainWindow { DataContext = main };
            Current.MainWindow = window;
            window.Show();
        }
    }
}
