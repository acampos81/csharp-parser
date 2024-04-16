using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpParser
{
  public class XmlCompiler
  {
    private ITokenizer _tokenizer;
    private StringBuilder _tagBuilder;

    public XmlCompiler(ITokenizer tokenizer)
    {
      _tokenizer = tokenizer;
      _tagBuilder = new StringBuilder();
    }

    public async Task Start()
    {
      CompileStatements(_tagBuilder, 0);

      if(_tagBuilder.Length > 0)
      {
        Console.WriteLine(_tagBuilder.ToString());
        _tagBuilder.Clear();
      }
    }

    public void CompileStatements(StringBuilder tagBuilder, int depth)
    {
      if(_tokenizer.HasMoreTokens())
      {
        _tokenizer.Advance();

        try
        {
          if( CompileUsingDirective(tagBuilder, depth) ||
              CompileNamespaceDeclaration(tagBuilder, depth) ||
              CompileClassDeclaration(tagBuilder, depth))
          {
            CompileStatements(tagBuilder, depth);
          }
        }
        catch(Exception e)
        {
          Console.WriteLine(e);
        }
      }
    }

    public void CompileStatementSequence()
    {

    }

    public bool CompileUsingDirective(StringBuilder tagBuilder, int depth)
    {
      TokenType   tt = _tokenizer.CurrentTokenType();
      KeywordType kt = _tokenizer.CurrentKeywordType();

      if(tt == TokenType.KEYWORD && kt == KeywordType.USING)
      {
        string kWord = _tokenizer.CurrentValue<string>();

        // <directive>
        OpenTag(tagBuilder, "directive", depth, inLine:false);

        InLineTags(tagBuilder, tt.ToString(), kWord, depth+1);

        AdvanceStreamWithException();

        bool success = CompileIdentifier(tagBuilder, depth+1);

        AdvanceStreamWithException();

        success &= CompileSymbol(tagBuilder, ';', depth+1);

        if(success == false)
        {
          throw new Exception(Constants.UnexpectedSyntaxError);
        }

        // </directive>
        CloseTag(tagBuilder, "directive", depth);

        return true;
      }
      
      return false;
    }

    public void CompileUsingAlias()
    {

    }

    public bool CompileNamespaceDeclaration(StringBuilder tagBuilder, int depth)
    {
      TokenType   tt = _tokenizer.CurrentTokenType();
      KeywordType kt = _tokenizer.CurrentKeywordType();

      if(tt == TokenType.KEYWORD && kt == KeywordType.NAMESPACE)
      {
        string ktStr = kt.ToString();
        string kWord = _tokenizer.CurrentValue<string>();

        // <namespace>
        OpenTag(tagBuilder, ktStr, depth, inLine:false);

        InLineTags(tagBuilder, tt.ToString(), kWord, depth+1);

        AdvanceStreamWithException();

        bool success = true;
        
        success &= CompileIdentifier(tagBuilder, depth+1);

        AdvanceStreamWithException();

        success &= CompileSymbol(tagBuilder, '{', depth+1);

        CompileStatements(tagBuilder, depth+1);

        success &= CompileSymbol(tagBuilder, '}', depth+1);

        if(success == false)
        {
          throw new Exception(Constants.UnexpectedSyntaxError);
        }

        // </namespace>
        CloseTag(tagBuilder, ktStr, depth);

        return true;
      }

      return false;
    }

    public bool CompileClassDeclaration(StringBuilder tagBuilder, int depth)
    {
      TokenType   tt = _tokenizer.CurrentTokenType();

      if(tt == TokenType.KEYWORD)
      {
        // <class>
        OpenTag(tagBuilder, KeywordType.CLASS.ToString(), depth, false);

        /* Class declarations in C# support a mix of access, and class modifiers (i.e. "partial static public", or "static internal")
         * A comprehensive compilation of legal combinations is beyond the scope of this version of the parser. For the time being
         * modifers are gathered in the order they're found before the "class" keyword. */
        KeywordType kt = _tokenizer.CurrentKeywordType();
        while(IsAccessModifier(kt) || IsClassModifier(kt))
        {
          string kStr = _tokenizer.CurrentValue<string>();
          InLineTags(tagBuilder, tt.ToString(), kStr, depth+1);
          AdvanceStreamWithException();
          kt = _tokenizer.CurrentKeywordType();
        }

        bool success = CompileKeyWord(tagBuilder, KeywordType.CLASS, depth+1);

        AdvanceStreamWithException();

        success &= CompileIdentifier(tagBuilder, depth+1);

        AdvanceStreamWithException();

        // Derived type compilation
        if(CompileSymbol(tagBuilder, ':', depth+1))
        {
          AdvanceStreamWithException();

          success &= CompileIdentifier(tagBuilder, depth+1);

          AdvanceStreamWithException();
        }

        success &= CompileSymbol(tagBuilder, '{', depth+1);
        
        CompileStatements(tagBuilder, depth+1);

        success &= CompileSymbol(tagBuilder, '}', depth+1);

        if(success == false)
        {
          throw new Exception(Constants.UnexpectedSyntaxError);
        }

        // </class>
        CloseTag(tagBuilder, KeywordType.CLASS.ToString(), depth);
        return true;
      }

      return false;
    }

    private bool IsAccessModifier(KeywordType kt)
    {
      return
        kt == KeywordType.PUBLIC    ||
        kt == KeywordType.PRIVATE   ||
        kt == KeywordType.INTERNAL  ||
        kt == KeywordType.PROTECTED ||
        kt == KeywordType.SEALED;
    }

    private bool IsClassModifier(KeywordType kt)
    {
      return
        kt == KeywordType.STATIC  ||
        kt == KeywordType.PARTIAL ||
        kt == KeywordType.ABSTRACT;
    }

    private bool IsModifier(KeywordType kt)
    {
      return
        kt == KeywordType.STATIC   ||
        kt == KeywordType.ABSTRACT ||
        kt == KeywordType.CONST    ||
        kt == KeywordType.VIRTUAL;
    }

    public void CompileClassConstructor(StringBuilder tagBuilder, int depth)
    {

    }


    public void CompileVariableDeclaration()
    {
    }

    public bool CompileIdentifier(StringBuilder tagBuilder, int depth)
    {
      TokenType tt = _tokenizer.CurrentTokenType();

      if(tt == TokenType.IDENTIFIER)
      {
        StringBuilder idBuilder = new StringBuilder();
        BuildIdentifier(idBuilder, TokenType.IDENTIFIER);
        InLineTags(tagBuilder, tt.ToString(), idBuilder.ToString(), depth);
        return true;
      }

      return false;
    }

    private void BuildIdentifier(StringBuilder idBuilder, TokenType expectedTokenType)
    {
      if(expectedTokenType == TokenType.IDENTIFIER)
      {
        idBuilder.Append(_tokenizer.CurrentValue<string>());
        if(_tokenizer.HasMoreTokens())
        {
          if(_tokenizer.NextTokenType() == TokenType.SYMBOL)
          {
            char c = _tokenizer.NextValue<char>();
            if(c.Equals('.'))
            {
              _tokenizer.Advance();
              BuildIdentifier(idBuilder, TokenType.SYMBOL);
            }
          }
        }
      }
      else if(expectedTokenType == TokenType.SYMBOL)
      {
        idBuilder.Append(_tokenizer.CurrentValue<char>());
        if(_tokenizer.HasMoreTokens())
        {
          if(_tokenizer.NextTokenType() == TokenType.IDENTIFIER)
          {
            _tokenizer.Advance();
            BuildIdentifier(idBuilder, TokenType.IDENTIFIER);
          }
        }
      }
    }

    public void CompileFunctionDeclaration()
    {
    }

    public void CompileFunctionBody()
    {
    }

    public void CompileParameterList()
    {
    }

    public void CompileIfStatement()
    {
    }

    public void CompileSwitchStatement()
    {
    }

    public void CompileForStatement()
    {
    }

    public void CompileForEachStatement()
    {
    }

    public void CompileWhileStatement()
    {
    }

    public void CompileDoStatement()
    {
    }

    public void CompileTryStatement()
    {
    }

    public void CompileExpression()
    {
    }

    public void CompileReturn()
    {
    }

    private bool CompileKeyWord(StringBuilder tagBuilder, KeywordType keywordType, int depth)
    {
      TokenType   tt = _tokenizer.CurrentTokenType();
      KeywordType kt = _tokenizer.CurrentKeywordType();

      if(tt == TokenType.KEYWORD && kt == keywordType)
      {
        string kStr = _tokenizer.CurrentValue<string>();
        InLineTags(tagBuilder, tt.ToString(), kStr, depth);
        return true;
      }

      return false;
    }

    private bool CompileSymbol(StringBuilder tagBuilder, char symbol, int depth)
    {
      TokenType tt = _tokenizer.CurrentTokenType();
      char      cs = _tokenizer.CurrentValue<char>();

      if(tt == TokenType.SYMBOL && cs.Equals(symbol))
      {
        InLineTags(tagBuilder, tt.ToString(), cs.ToString(), depth);
        return true;
      }

      return false;
    }

    private void OpenTag(StringBuilder tagBuilder, string tagName, int depth, bool inLine)
    {
      AppendSpaces(tagBuilder, depth);
      tagBuilder.Append($"<{tagName.ToLower()}>");
      if(inLine == false) tagBuilder.Append("\n");
    }

    private void CloseTag(StringBuilder tagBuilder, string tagName, int depth)
    {
      AppendSpaces(tagBuilder, depth);
      tagBuilder.Append($"</{tagName.ToLower()}>");
      tagBuilder.Append("\n");
    }

    private void InLineTags(StringBuilder tagBuilder, string tagName, string value, int depth)
    {
      OpenTag(tagBuilder, tagName, depth, inLine:true);
      tagBuilder.Append($" {EscapeValue(value)} ");
      CloseTag(tagBuilder, tagName, 0);
    }

    private void AppendSpaces(StringBuilder tagBuilder, int depth)
    {
      for(int i=0; i<depth; i++)
      {
        // double space
        tagBuilder.Append("  ");
      }
    }

    private string EscapeValue(string value)
    {
      switch(value)
      {
        case "<":
          return "&lt;";
        case ">":
          return "&gt;";
        case "&":
          return "&amp;";
        default:
          return value;
      }
    }

    private void AdvanceStreamWithException()
    {
      if(_tokenizer.HasMoreTokens())
      {
        _tokenizer.Advance();
      }
      else
      {
        throw new Exception(Constants.UnexpectedTokenStreamEnd);
      }
    }
  }
}

