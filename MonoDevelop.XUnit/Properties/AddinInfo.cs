using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"MonoDevelop.XUnit2", 
	Namespace = "MonoDevelop.XUnit",
	Version = "0.7.11"
)]

[assembly:AddinName ("xUnit.NET 2 testing framework support")]
[assembly:AddinCategory ("Testing")]
[assembly:AddinDescription ("Integrates xUnit.NET 2 into Visual Studio for Mac.")]
[assembly:AddinAuthor ("Sergey Khabibullin and other contributors")]
