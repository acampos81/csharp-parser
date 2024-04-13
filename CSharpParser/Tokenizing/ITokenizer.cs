using System.Threading.Tasks;

namespace CSharpParser
{
  public interface ITokenizer
  {
    Task      Start();
    bool      HasMoreTokens();
    void      Advance();
    TokenType GetTokenType();
    KeyWord   GetKeyWord();
    char      GetSymbol();
    string    GetIdentifier();
    int       GetIntValue();
    float     GetSingleValue();
    string    GetStringValue();
  }
}
