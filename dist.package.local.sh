msbuild MonoDevelop.UnitTesting.XUnit.sln /t:clean
msbuild MonoDevelop.UnitTesting.XUnit.sln
mono /Applications/Visual\ Studio.app/Contents/Resources/lib/monodevelop/bin/vstool.exe setup pack MonoDevelop.XUnit/bin/Debug/MonoDevelop.XUnit.dll