using System;
using System.IO;

namespace CSharpParser
{
  class Program
  {
    static void Main(string[] args)
    {
      if(args.Length != 1)
      {
        Console.WriteLine("Invalid number of arguments. Must provide a file path, or directory path.");
        return;
      }

      for (int i = 0; i < args.Length; i++)
      {
        string path = args[i];

        if(File.Exists(path))
        {
          if(path.EndsWith(".cs"))
          {

          }
        }
        else if(Directory.Exists(path))
        {

        }
      }
    }
  }
}
