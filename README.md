GroupPolicy.Parser
==================

Group Policy Registry.pol Parser


Example:

using GroupPolicy.Parser;
var file = new RegistryFile();
file.Open("C:\\backup\\registry.pol");

foreach(var setting in file.Settings) 
{
  setting.Data = "Test";
}

file.Save();


RegistryFile:
Open(string path);
Item(int index);
Save();

int Count
string Path
RegistrySetting[] Settings

RegistrySetting:
string KeyPath
string Value
uint Type (RegistryValueType)
uint Size
byte[] BinaryData
object Data
