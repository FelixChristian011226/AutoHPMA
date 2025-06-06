﻿using MicaSetup.Helper;
using System.Windows;

namespace MicaSetup.Design.Controls;

public static class MessageBox
{
    public static WindowDialogResult Info(DependencyObject dependencyObject, string message)
    {
        Window owner = (dependencyObject is Window win ? win : dependencyObject == null ? ApplicationDispatcherHelper.MainWindow : Window.GetWindow(dependencyObject)) ?? ApplicationDispatcherHelper.MainWindow;

        return new MessageBoxDialog()
        {
            Type = MessageBoxType.Info,
            Message = message,
        }.ShowDialog(owner);
    }

    public static WindowDialogResult Question(DependencyObject dependencyObject, string message)
    {
        Window owner = (dependencyObject is Window win ? win : dependencyObject == null ? ApplicationDispatcherHelper.MainWindow : Window.GetWindow(dependencyObject)) ?? ApplicationDispatcherHelper.MainWindow;

        return new MessageBoxDialog()
        {
            Type = MessageBoxType.Question,
            Message = message,
        }.ShowDialog(owner);
    }
}
