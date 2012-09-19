using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("IronLua")]
[assembly: AssemblyDescription("IronLua - A Lua runtime for .NET")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("IronLua Team")]
[assembly: AssemblyProduct("IronLua")]
[assembly: AssemblyCopyright("IronLua Contributors")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e3fe6c70-21d3-4ebd-aa13-fbfb0f78b948")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion(IronLua.CurrentVersion.AssemblyVersion)]
[assembly: AssemblyFileVersion(IronLua.CurrentVersion.AssemblyFileVersion)]
[assembly: AssemblyInformationalVersion(IronLua.CurrentVersion.AssemblyInformationalVersion)]

[assembly:InternalsVisibleTo("IronLua.Tests")]
