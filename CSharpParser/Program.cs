﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpParser
{
  class Program
  {
    static async Task Main(string[] args)
    {
      //CreateFiles();

      //*
      List<Task> tokenizationTasks = new List<Task>();
      for (int i = 0; i < args.Length; i++)
      {
        string path = args[i];

        if (File.Exists(path))
        {
          if (path.EndsWith(".cs"))
          {
            tokenizationTasks.Add(Tokenize(path));
          }
        }
        else if (Directory.Exists(path))
        {
          string[] paths = Directory.GetFiles(path);
          for (int j = 0; j < paths.Length; j++)
          {
            tokenizationTasks.Add(Tokenize(path));
          }
        }
      }

      while(tokenizationTasks.Count > 0)
      {
        Task task = await Task.WhenAny(tokenizationTasks);
        await task;
      }
      //*/
    }

    static async Task Tokenize(string filePath)
    {
      ITokenizer tokenizer = new Tokenizer(filePath);
      await tokenizer.Start().ConfigureAwait(false);
    }

    static void CreateFiles(string[] args)
    {
      string path = args[0];

      using (FileStream fs = File.Create("tester0.txt"))
      {
        StringBuilder sb = new StringBuilder();
        sb.Append("A:");
        for(int i=0; i<10000; i++)
        {
          sb.Append($"{i},");
        }
        // writing data in string
        byte[] info = new UTF8Encoding(true).GetBytes(sb.ToString());
        fs.Write(info, 0, info.Length);
      }

      using (FileStream fs = File.Create("tester1.txt")) 
      {
        StringBuilder sb = new StringBuilder();
        sb.Append("B:");
        for(int i=0; i<10000000; i++)
        {
          sb.Append($"{i},");
        }
        // writing data in string
        byte[] info = new UTF8Encoding(true).GetBytes(sb.ToString());
        fs.Write(info, 0, info.Length);
      }

      using (FileStream fs = File.Create("tester2.txt")) 
      {
        StringBuilder sb = new StringBuilder();
        sb.Append("C:");
        for(int i=0; i<100000; i++)
        {
          sb.Append($"{i},");
        }
        // writing data in string
        byte[] info = new UTF8Encoding(true).GetBytes(sb.ToString());
        fs.Write(info, 0, info.Length);
      }

      using (FileStream fs = File.Create("tester3.txt")) 
      {
        StringBuilder sb = new StringBuilder();
        sb.Append("D:");
        for(int i=0; i<1000000; i++)
        {
          sb.Append($"{i},");
        }
        // writing data in string
        byte[] info = new UTF8Encoding(true).GetBytes(sb.ToString());
        fs.Write(info, 0, info.Length);
      }
    }
  }
}
