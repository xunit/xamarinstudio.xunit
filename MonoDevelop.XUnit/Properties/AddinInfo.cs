using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"MonoDevelop.XUnit2", 
	Namespace = "MonoDevelop.XUnit",
	Version = "0.5.6"
)]

[assembly:AddinName ("xUnit.NET 2 testing framework support")]
[assembly:AddinCategory ("Testing")]
[assembly:AddinDescription ("Integrates xUnit.NET 2 into the MonoDevelop / Xamarin Studio IDE.")]
[assembly:AddinAuthor ("Sergey Khabibullin/Lex Li")]

