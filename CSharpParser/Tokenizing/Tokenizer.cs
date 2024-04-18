using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace CSharpParser
{
  class Tokenizer : ITokenizer
  {
    private class Token
    {
      public TokenType type;
      public KeywordType keyword;
      public object value;
    }

    private List<Token> _tokens;
    private Token _currentToken;
    private CommentType _commentType;

    #region Interface Methods
    public async Task Start(string filePath)
    {
      using(StreamReader reader = new StreamReader(filePath))
      {
        string line;
        _tokens = new List<Token>();
        StringBuilder sb = new StringBuilder();

        while (reader.EndOfStream == false)
        {
          line = await reader.ReadLineAsync().ConfigureAwait(false);

          if(string.IsNullOrEmpty(line)) continue;

          for(int i=0; i<line.Length; i++)
          {
            char c = line[i];

            // If currently in a comment block (/*...*/) check if the current and next characters
            // are the end of the block (*/).  If not, continue ignoring characters.
            if(_commentType == CommentType.BLOCK)
            {
              if(IsBlockCommentEnd(line, i, c))
              {
                _commentType = CommentType.NONE;
              }

              continue;
            }
            
            // If the current and next characters signal the start of a comment, handle according
            // to the comment type.
            if(IsCommentStart(line, i, c, out _commentType))
            {
              if(_commentType == CommentType.LINE)
              {
                // If it's a line comment (//), tokenize the string buffer if greater than 0, and move on to the next line.
                if(sb.Length > 0)
                {
                  TokenizeString(sb);
                }
                break;
              }
              else
              {
                // If it's a block comment (/*...*/) continue evaluating characters on this and subsequent lines,
                // looking for the end characters (*/) until the end of the current line.
                continue;
              }
            }

            if(IsWhiteSpace(c))
            {
              if(sb.Length > 0)
              {
                TokenizeString(sb);
              }
            }
            else if(IsSymbol(c))
            {
              if(sb.Length > 0)
              {
                TokenizeString(sb);
              }

              TokenizeSymbol(c);
            }
            else
            {
              sb.Append(c);
            }
          }

          // Tokenize any string remaining in the buffer after the current line loop ends
          if(sb.Length > 0)
          {
            TokenizeString(sb);
          }

          // If the last line was, or contained part of a line comment, reset the comment
          // type to none since line comments don't carry over to subsequent lines.
          if(_commentType == CommentType.LINE)
          {
            _commentType = CommentType.NONE;
          }
        }
      }
    }

    public bool HasMoreTokens()
    {
      return _tokens.Count > 0;
    }

    public void Advance()
    {
      _currentToken = _tokens[0];
      _tokens.RemoveAt(0);
    }

    public TokenType GetTokenType()
    {
      return _currentToken.type;
    }

    public KeywordType GetKeywordType()
    {
      return _currentToken.keyword;
    }

    public T GetValue<T>()
    {
      return (T)_currentToken.value;
    }

    public bool HasTokenAt(int index)
    {
      return index < _tokens.Count;
    }

    public TokenType LookAheadTokenType(int index = 0)
    {
      return HasTokenAt(index) ? _tokens[index].type : TokenType.NONE;
    }

    public KeywordType LookAheadKeywordType(int index = 0)
    {
      return HasTokenAt(index) ? _tokens[index].keyword : KeywordType.NONE;
    }

    public T LookAheadValue<T>(int index = 0)
    {
      return HasTokenAt(index) ? (T)_tokens[index].value : default(T);
    }
    #endregion

    #region Tokenizing
    private void TokenizeString(StringBuilder sb)
    {
      string strValue = sb.ToString();
      sb.Clear();

      Token t = new Token() { value = strValue };

      if(IsKeyword(strValue))
      {
        t.keyword = Enum.Parse<KeywordType>(strValue, true);
        t.type = TokenType.KEYWORD;
      }
      else if(IsNumConst(strValue))
      {
        t.type = TokenType.NUMBER;
      }
      else if(IsStringConst(strValue))
      {
        t.type = TokenType.STRING;
      }
      else
      {
        t.type = TokenType.IDENTIFIER;
      }

      _tokens.Add(t);
    }

    private void TokenizeSymbol(char c)
    {
      _tokens.Add(new Token { type = TokenType.SYMBOL, value = c});
    }
    #endregion

    #region Validators
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

    private bool IsKeyword(string str)
    {
      for(int i=0; i<Grammar.keywords.Length; i++)
      {
        if (str.Equals(Grammar.keywords[i])) return true;
      }
      return false;
    }

    private bool IsNumConst(string str)
    {
      return int.TryParse(str, out _);
    }

    private bool IsStringConst(string str)
    {
      return str.StartsWith('"') && str.EndsWith('"');
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
    #endregion
  }
}
