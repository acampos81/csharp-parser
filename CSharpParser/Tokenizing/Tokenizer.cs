using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CSharpParser
{
  class Tokenizer : ITokenizer
  {
    private struct Token
    {
      public TokenType type;
      public KeyWord keyword;
      public object value;
    }

    private enum CommentType
    {
      NONE,
      LINE,
      BLOCK
    }

    private string _filePath;
    private Queue<Token> _tokens;
    private Token _currentToken;
    private CommentType _commentType;


    public Tokenizer(string filePath)
    {
      _filePath = filePath;
    }
  

    public async Task Start()
    {
      using(StreamReader reader = new StreamReader(_filePath))
      {
        string line;
        _tokens = new Queue<Token>();
        StringBuilder sb = new StringBuilder();

        while (reader.EndOfStream == false)
        {
          line = await reader.ReadLineAsync().ConfigureAwait(false);

          if(string.IsNullOrEmpty(line)) continue;

          for(int i=0; i<line.Length; i++)
          {
            char c = line[i];

            if(_commentType == CommentType.BLOCK)
            {
              if(IsBlockCommentEnd(line, i, c))
              {
                _commentType = CommentType.NONE;
              }

              continue;
            }
            
            if(IsCommentStart(line, i, c, out CommentType ct))
            {
              _commentType = ct;
              if(_commentType == CommentType.LINE)
              {
                break;
              }
              else
              {
                continue;
              }
            }

            bool isWhiteSpace = IsWhiteSpace(c);
            bool isSymbol = IsSymbol(c);
            bool isEndOfLine = i == line.Length-1;

            if(isWhiteSpace || isSymbol || isEndOfLine)
            {
              if(sb.Length > 0)
              {
                if(isSymbol == false) sb.Append(c);

                _tokens.Enqueue(new Token { value = sb.ToString() });
                sb.Clear();
              }

              if(isSymbol)
              {
                _tokens.Enqueue(new Token { value = c });
              }
            }
            else
            {
              sb.Append(c);
            }
          }

          sb.Clear();

          if(_commentType == CommentType.LINE)
          {
            _commentType = CommentType.NONE;
          }
        }
      }

      while(_tokens.Count > 0)
      {
        Token t = _tokens.Dequeue();
        Console.WriteLine(t.value);
      }
    }

    public bool HasMoreTokens()
    {
      return _tokens.Count > 0;
    }

    public void Advance()
    {
      _currentToken = _tokens.Dequeue();
    }

    public TokenType GetTokenType()
    {
      return _currentToken.type;
    }

    public KeyWord GetKeyWord()
    {
      return _currentToken.keyword;
    }

    public char GetSymbol()
    {
      return (char)_currentToken.value;
    }

    public string GetIdentifier()
    {
      return (string)_currentToken.value;
    }

    public int GetIntValue()
    {
      return (int)_currentToken.value;
    }

    public float GetSingleValue()
    {
      return (float)_currentToken.value;
    }

    public string GetStringValue()
    {
      return (string)_currentToken.value;
    }

    private bool IsWhiteSpace(char c)
    {
      for (int i = 0; i < Grammar.whiteSpace.Length; i++)
      {
        if (c.Equals(Grammar.whiteSpace[i])) return true;
      }
      return false;
    }

    private bool IsSymbol(char c)
    {
      for (int i = 0; i < Grammar.symbols.Length; i++)
      {
        if (c.Equals(Grammar.symbols[i])) return true;
      }
      return false;
    }

    private bool IsCommentStart(string line, int charIndex, char currentChar, out CommentType commentType)
    {
      commentType = CommentType.NONE;

      bool isForwardSlash = currentChar.Equals('/');

      if (isForwardSlash == false || charIndex == line.Length - 1) return false;

      char nextChar = line[charIndex + 1];

      if (nextChar.Equals('/'))
      {
        commentType = CommentType.LINE;
        return true;
      }
      else if (nextChar.Equals('*'))
      {
        commentType = CommentType.BLOCK;
        return true;
      }
      else
      {
        return false;
      }
    }

    private bool IsBlockCommentEnd(string line, int charIndex, char currentChar)
    {
      bool isForwardSlash = currentChar.Equals('/');

      if (isForwardSlash == false || charIndex == 0) return false;

      char prevChar = line[charIndex - 1];

      if (prevChar.Equals('*'))
      {
        return true;
      }
      else
      {
        return false;
      }
    }
  }
}
