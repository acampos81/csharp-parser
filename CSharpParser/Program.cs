using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CSharpParser
{
  class Program
  {
    static async Task Main(string[] args)
    {
      List<Task<ITokenizer>> tokenizationTasks = new List<Task<ITokenizer>>();
      for (int i = 0; i < args.Length; i++)
      {
        string path = args[i];

        if (File.Exists(path))
        {
          if (path.EndsWith(".cs"))
          {
            ITokenizer tokenizer = new Tokenizer();
            await tokenizer.Start(path).ConfigureAwait(false);

            XmlCompiler compiler = new XmlCompiler(tokenizer);
            await compiler.Start();
          }
        }
      }
    }
  }
}
