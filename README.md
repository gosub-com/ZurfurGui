# Zurfur Gui

While working on [Zurfur](https://github.com/gosub-com/Zurfur) and
porting the GUI from WinForms to Avalonia, I got to thinking what it would be
like to implement a GUI without using inheritance.  I also wanted to see how big
a minimal C# WebAssembly GUI would be.  So far, the full download is only about 
2.2 megabytes of compressed data.  

You can see the result here: https://gosub.com/zurfurgui

## Design Goals

Zurfur Gui is an experiment to see what a minimal C# WebAssembly `Canvas` GUI might look like.

* Runs in browsers and natively on Windows
* Quick and easy to whip up small GUI applications (tools, etc.)
* Uses JSON instead of XML
* Small download - uses built-in platform drawing API (i.e. `Canvas`)




