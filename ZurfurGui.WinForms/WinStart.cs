﻿using ZurfurGui.Windows;

namespace ZurfurGui.WinForms;

public static class WinStart
{
    public static void Start(Action<AppWindow> mainAppEntry)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        Application.Run(new FormZurfurGui(mainAppEntry));
    }
}
