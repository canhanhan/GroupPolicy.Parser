# GroupPolicy.Parser

## Example
```C#
using GroupPolicy.Parser;

var file = new RegistryFile();
file.Open("C:\\backup\\registry.pol");

foreach(var setting in file.Settings) 
{
  setting.Data = "Test";
}

file.Save(); 
```

## Reference
### RegistryFile
#### Methods
* Open(string path)
* Item(int index)
* Save()

#### Properties
* int Count
* string Path
* RegistrySetting[] Settings

### RegistrySetting
#### Properties
* string KeyPath
* string Value
* uint Type (RegistryValueType)
* uint Size
* byte[] BinaryData
* object Data
