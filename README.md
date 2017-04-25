[![Stories in Ready](https://badge.waffle.io/xunit/xamarinstudio.xunit.svg?label=ready&title=Ready)](http://waffle.io/xunit/xamarinstudio.xunit) 

xUnit.NET testing framework support
===================================
Integrates xUnit.NET into the MonoDevelop / Xamarin Studio IDE.

Copyright (c) Sergey Khabibullin/Lex Li/Zheng Wang

MonoDevelop/Xamarin Studio Installation
---------------------------------------
1. Open MonoDevelop or Xamarin Studio.
1. Use Add-ins... menu item to launch Add-in Manager.
1. Click Gallery tag, and make sure "All repositories" is selected as Repository.
1. Choose "xUnit.NET 2 testing framework support" under Testing, and click Install button.

Visual Studio for Mac Installation
----------------------------------
> IMPORTANT: Starting from v0.7.3, please download the .mpack files from https://github.com/xunit/xamarinstudio.xunit/releases
> Then in Extension Manager you can use "Install from file..." button to manually install this extension.

1. Open Visual Studio for Mac.
1. Use Extensions... menu item to launch Extension Manager.
1. Click Gallery tag, and make sure "All repositories" is selected as Repository.
1. Choose "xUnit.NET 2 testing framework support" under Testing, and click Install button.

Usage
-----
Upon installation, the Unit Tests panel in Xamarin Studio/MonoDevelop/Visual Studio for Mac starts to recognize xUnit.net test cases. Then you can run/debug test cases.

Extra Note
----------
* If you install 5.x version of this add-in in MonoDevelop/Xamarin Studio 5.x and then upgrade the IDE to 6.x, please use Add-in Manager to upgrade this add-in to 6.x version.
* If Add-in Manager asks you to install/update this add-in to 0.6.x in MonoDevelop/Xamarin Studio 5.x, please manually download the 0.5.x build and install it.
* The add-in can be manually downloaded from [here](http://addins.monodevelop.com/Project/Index/220) if you cannot use Add-in Manager.
* Anyone who wants to contribute to this project, please check [the contributing guide](https://github.com/xunit/xamarinstudio.xunit/blob/master/CONTRIBUTING.md) to see how to set up the development environment.
