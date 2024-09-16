# Zurfur Gui

While working on the [Zurfur Compiler](https://github.com/gosub-com/Zurfur) and
porting the GUI from WinForms to Avalonia, I got to thinking what it would be
like to implement a GUI without using inheritance.  I also wanted to see how big
a minimal C# WebAssembly GUI would be.  So far, the full download is only about 
2.2 megabytes of compressed data.  

You can see the result here: https://gosub.com/zurfurgui

## Design Goals

Zurfur Gui is a minimal GUI taking inspiration from WinForms, WPF, and Avalonia.
It is a C# layer on top of Javascript's Canvas element.  It uses Canvas primatives
to render shapes, images, and text (i.e. `HTMLCanvasElement.getContext().fillText`).

* Runs in a browser via WebAssembly [Click Here](https://gosub.com/zurfurgui)
* Runs natively on Windows to allow easy debugging
* Replacement for the [Avalonia Zurfur IDE](https://gosub.com/zurfur)
* No inheritance
* Small download (use Javascript primitives as much as possible)
* Eventual import/export and code binding generation via JSON (no XML)




