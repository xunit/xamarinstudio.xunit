A Short History of This Project
===============================

xUnit support has been requested for a long time by MonoDevelop/Xamarin Studio users, but it wasn't available until Sergey Khabibullin implemented an addin
during Google Summer of Code 2014 under Xamarin's supervision.

Later this addin project has been accepted by xUnit guys and becomes part of xUnit family at GitHub. However, this addin was made for xUnit 1.x. After release 
of xUnit 2.x, many issues had been found while Sergey could not continue to work on this addin.

Lex Li started to review this project in Jan 2016, and then finished his initial experiments in Feb. He took over this project officially on May 23. Zheng Wang, 
Matt Ward, and Jose Medrano contributed important patches to move this project forward. Mikayla Hutchinson from Xamarin also kindly shared her advice to guide 
the development.

This addin works fine with MonoDevelop 7, and Visual Studio for Mac. Community members are welcome to raise issues and send patches at GitHub.

Microsoft open sourced the Visual Studio unit testing infrastructure as VSTest, and since Visual Studio for Mac 7.3 VSTest was ported. As a result, this extension is no longer needed. Thus, this marks the end of this project.

References
----------
* https://forums.xamarin.com/discussion/17864/how-i-can-run-xunit-tests-from-xamarin-studio
* https://groups.google.com/forum/#!topic/mono-soc-2014/aJXSzqjdXPI
* https://github.com/xunit/xamarinstudio.xunit
* https://github.com/xunit/xamarinstudio.xunit/issues/6
* https://blogs.msdn.microsoft.com/visualstudio/2017/12/04/visual-studio-2017-version-15-5-visual-studio-for-mac-released/
