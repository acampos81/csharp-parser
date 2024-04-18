using System.Threading.Tasks;

namespace CSharpParser
{
  public interface ITokenizer
  {
    bool        HasMoreTokens();
    void        Advance();
    TokenType   GetTokenType();
    KeywordType GetKeywordType();
    T           GetValue<T>();
    bool        HasTokenAt(int index);
    TokenType   LookAheadTokenType(int index = 0);
    KeywordType LookAheadKeywordType(int index = 0);
    T           LookAheadValue<T>(int index = 0);
    Task        Start(string filePath);
  }
}
