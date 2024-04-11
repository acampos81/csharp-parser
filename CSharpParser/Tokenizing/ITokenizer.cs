using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpParser
{
  public interface ITokenizer
  {
    void      Advance();
    bool      HasMoreTokens();
    TokenType GetTokenType();
    KeyWord   GetKeyWord();
    char      GetSymbol();
    string    GetIdentifier();
    int       GetIntValue();
    float     GetSingleValue();
    string    GetStringValue();
  }
}
