using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin (
	"MonoDevelop.XUnit", 
	Namespace = "MonoDevelop.XUnit",
	Version = "0.5"
)]

[assembly:AddinName ("xUnit.NET testing framework support")]
[assembly:AddinCategory ("Testing")]
[assembly:AddinDescription ("Integrates xUnit.NET into the MonoDevelop / Xamarin Studio IDE.")]
[assembly:AddinAuthor ("Sergey Khabibullin")]

