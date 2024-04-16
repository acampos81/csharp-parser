using System.Threading.Tasks;

namespace CSharpParser
{
  public interface ITokenizer
  {
    bool        HasMoreTokens();
    void        Advance();
    TokenType   CurrentTokenType();
    TokenType   NextTokenType();
    KeywordType CurrentKeywordType();
    KeywordType NextKeywordType();
    T           CurrentValue<T>();
    T           NextValue<T>();
    Task        Start(string filePath);
  }
}
