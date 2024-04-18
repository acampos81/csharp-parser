# csharp-parser

The purpose of this parser is to analyze C# objects from a .cs file and produce XML output that breaks up their structure for further inspection. As a disclaimer I have to point out this is not a fully featured C# parser. It only supports the following set of language features:

* using directives
* namespace declarations
* class declarations
* class variable declarations
* class constructor declarations
* class functions
* function parameter lists
* function bodies
* local variables
* if statements
* for loops

### Usage Example
```dos
C:> .\CSharpParser.exe SmallClass.cs
```
The following two files are provided for testing. They can be found in the `CSharpParser/TestClasses/` directory.

* MultiClass.cs
* SmallClass.cs


### Output Example

Below is an example of the the output produced from parsing a small class like the one pictured below.  If multiple classes are contained within one file, the output will contain a combined XML tree of all classes analyzed.

```csharp
using System;

public class SmallClass
{
  private int _intValue;

  public SmallClass()
  {
  }
}
```


```xml
<directive>
  <keyword> using </keyword>
  <identifier> System </identifier>
  <symbol> ; </symbol>
</directive>
<class>
  <keyword> public </keyword>
  <keyword> class </keyword>
  <identifier> SmallClass </identifier>
  <statementsequence>
    <symbol> { </symbol>
      <variable>
        <keyword> private </keyword>
        <keyword> int </keyword>
        <identifier> _intValue </identifier>
        <symbol> ; </symbol>
      </variable>
      <constructor>
        <keyword> public </keyword>
        <identifier> SmallClass </identifier>
          <parameters>
          <symbol> ( </symbol>
          <symbol> ) </symbol>
          </parameters>
        <statementsequence>
          <symbol> { </symbol>
          <symbol> } </symbol>
        </statementsequence>
      </constructor>
    <symbol> } </symbol>
  </statementsequence>
</class>
```