# Zurfur Gui

While working on [Zurfur](https://github.com/gosub-com/Zurfur) and
porting the GUI from WinForms to Avalonia, I got to thinking what it would be
like to implement a GUI without using inheritance.  I also wanted to see how big
a minimal C# WebAssembly GUI would be.  So far, the full download is only about 
2.2 megabytes of compressed data.  

You can see the result here: https://gosub.com/zurfurgui

## Design Goals

Zurfur Gui is a minimal GUI taking inspiration from WinForms, WPF, and Avalonia.
It is a C# layer on top of Javascript's Canvas element. 

* Runs in browsers and also natively on Windows for easy debugging
* Small download - uses built-in platform drawing API (i.e. canvas)
* No inheritance - will be portable to [Zurfur](https://github.com/gosub-com/Zurfur)
* Replacement for Avalonia Zurfur IDE, which you can see here https://gosub.com/zurfur
* Quick and easy to whip up small GUI applications (tools, etc.)




