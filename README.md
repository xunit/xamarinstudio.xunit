[![Stories in Ready](https://badge.waffle.io/xunit/xamarinstudio.xunit.svg?label=ready&title=Ready)](http://waffle.io/xunit/xamarinstudio.xunit) 

**IMPORTANT: Latest MonoDevelop 7 and Visual Studio for Mac uses VSTest to run xUnit.net test cases natively.
Thus, you no longer need an extension like this. Please uninstall this extension to avoid any conflict.**

**This project fulfilled its mission and officially ended on Dec 4, 2017.**

xUnit.NET testing framework support
===================================
Integrates xUnit.NET into the MonoDevelop / Visual Studio for Mac IDE.

Copyright (c) Sergey Khabibullin/Lex Li/Zheng Wang

The history about the development of this extension can be found [here](https://github.com/xunit/xamarinstudio.xunit/blob/7.0/HISTORY.md).

> Xamarin Studio is obsolete, so no longer supported by the latest release of this extension.
> MonoDevelop 5.x and 6.x releases are obsolete, so no longer supported by the latest release of this extension.

MonoDevelop 7 Installation
--------------------------
> IMPORTANT: Starting from v0.7.6, please download the .mpack files from https://github.com/xunit/xamarinstudio.xunit/releases
> Then in Extension Manager you can use "Install from file..." button to manually install this extension.

1. Open MonoDevelop.
1. Use Add-ins... menu item to launch Add-in Manager.
1. Click Gallery tag, and make sure "All repositories" is selected as Repository.
1. Choose "xUnit.NET 2 testing framework support" under Testing, and click Install button.

> You can install MonoDevelop 7 by following the documentation on monodevelop.com, but you might also read [this blog post](https://blog.lextudio.com/the-success-of-running-monodevelop-7-on-linux-a55f1469b1d1) to learn the alternative way.

Visual Studio for Mac Installation
----------------------------------
> IMPORTANT: Starting from v0.7.6, please download the .mpack files from https://github.com/xunit/xamarinstudio.xunit/releases so as to use the latest release.
> Then in Extension Manager you can use "Install from file..." button to manually install this extension.

1. Open Visual Studio for Mac.
1. Use Extensions... menu item to launch Extension Manager.
1. Click Gallery tag, and make sure "All repositories" is selected as Repository.
1. Choose "xUnit.NET 2 testing framework support" under Testing, and click Install button.

Usage
-----
Upon installation, the Unit Tests panel in MonoDevelop/Visual Studio for Mac starts to recognize xUnit.net test cases. Then you can run/debug test cases.

Extra Note
----------
* The add-in can be manually downloaded from [here](http://addins.monodevelop.com/Project/Index/220) if you cannot use Add-in Manager.
* **Running .NET Core based unit test projects in Visual Studio for Mac does not require this extension.** This extension is only needed if you want to run .NET Framework/Mono based unit test projects. **Thus, don't report .NET Core based issues to this repo.**
* Anyone who wants to contribute to this project, please check [the contributing guide](https://github.com/xunit/xamarinstudio.xunit/blob/7.0/CONTRIBUTING.md) to see how to set up the development environment.
