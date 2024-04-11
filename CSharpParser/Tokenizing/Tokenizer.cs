using System;

namespace CSharpParser
{
  class Tokenizer : ITokenizer
  {
    public Tokenizer(string filePath)
    {
    }

    public void Advance()
    {
      throw new NotImplementedException();
    }

    public string GetIdentifier()
    {
      throw new NotImplementedException();
    }

    public int GetIntValue()
    {
      throw new NotImplementedException();
    }

    public KeyWord GetKeyWord()
    {
      throw new NotImplementedException();
    }

    public float GetSingleValue()
    {
      throw new NotImplementedException();
    }

    public string GetStringValue()
    {
      throw new NotImplementedException();
    }

    public char GetSymbol()
    {
      throw new NotImplementedException();
    }

    public TokenType GetTokenType()
    {
      throw new NotImplementedException();
    }

    public bool HasMoreTokens()
    {
      throw new NotImplementedException();
    }
  }
}
