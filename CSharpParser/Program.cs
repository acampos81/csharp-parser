using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CSharpParser
{
  class Program
  {
    static async Task Main(string[] args)
    {
      // List of async tasks starter per valid file path.
      List<Task<ITokenizer>> tokenizationTasks = new List<Task<ITokenizer>>();

      for (int i = 0; i < args.Length; i++)
      {
        string path = args[i];

        // Add a tokenization task for file paths
        if (File.Exists(path))
        {
          if (path.EndsWith(".cs"))
          {
            tokenizationTasks.Add(Tokenize(path));
          }
        }
        // Otherwise, if provided a directory, iterate through the files to find any
        // valid files and add to the task list.
        else if (Directory.Exists(path))
        {
          string[] paths = Directory.GetFiles(path);
          for (int j = 0; j < paths.Length; j++)
          {
            tokenizationTasks.Add(Tokenize(path));
          }
        }
      }

      // If any tokenization tasks have been created, asynchronously await their completion before compiling them.
      while (tokenizationTasks.Count > 0)
      {
        Task<ITokenizer> tokenizerTask = await Task.WhenAny(tokenizationTasks);
        await tokenizerTask;

        XmlCompiler compiler = new XmlCompiler(tokenizerTask.Result);
        await compiler.Start().ConfigureAwait(false);
      }
    }

    static async Task<ITokenizer> Tokenize(string filePath)
    {
      ITokenizer tokenizer = new Tokenizer();
      await tokenizer.Start(filePath).ConfigureAwait(false);
      return tokenizer;
    }
  }
}
