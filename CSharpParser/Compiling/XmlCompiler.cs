using System;
using System.Collections.Generic;
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
      try
      {
        CompileClassStatements(_tagBuilder, 0);

        if(_tagBuilder.Length > 0)
        {
          Console.WriteLine(_tagBuilder.ToString());
          _tagBuilder.Clear();
        }
      }
      catch(Exception e)
      {
        Console.WriteLine(e);
      }
    }

    private void CompileClassStatements(StringBuilder tagBuilder, int depth)
    {
      if(_tokenizer.HasMoreTokens())
      {
        _tokenizer.Advance();

        if( 
            CompileUsingDirective(tagBuilder, depth)           ||
            CompileNamespaceDeclaration(tagBuilder, depth)     ||
            CompileClassDeclaration(tagBuilder, depth)         ||
            CompileClassConstructor(tagBuilder, depth)         ||
            CompileClassVariableDeclaration(tagBuilder, depth) ||
            CompileFunctionDeclaration(tagBuilder, depth)      ||
            CompileLocalVariableDeclaration(tagBuilder, depth) ||
            //CompileIfStatement(tagBuilder, depth)              ||
            //CompileForLoop(tagBuilder, depth)                  ||
            CompileFunctionCall(tagBuilder, depth)
            )
        {
          CompileClassStatements(tagBuilder, depth);
        }
      }
    }

    private void CompileFunctionStatements(StringBuilder tagBuilder, int depth)
    {

    }

    private bool CompileStatementSequence(StringBuilder tagBuilder, int depth)
    {
      TokenType tt = _tokenizer.GetTokenType();

      if(tt == TokenType.SYMBOL)
      {
        char c = _tokenizer.GetValue<char>();

        if(c.Equals('{'))
        {
          StringBuilder localBuilder = new StringBuilder();

          OpenTag(localBuilder, "statementSequence", depth, false);

          if(CompileSymbol(localBuilder, '{', depth+1) == false)
          {
            throw new Exception(Constants.SyntaxError);
          }

          CompileClassStatements(localBuilder, depth+1);

          if(CompileSymbol(localBuilder, '}', depth+1) == false)
          {
            throw new Exception(Constants.SyntaxError);
          }

          CloseTag(localBuilder, "statementSequence", depth);

          tagBuilder.Append(localBuilder.ToString());

          localBuilder.Clear();

          return true;
        }
      }

      return false;
    }

    private bool CompileUsingDirective(StringBuilder tagBuilder, int depth)
    {
      TokenType   tt = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      if(tt == TokenType.KEYWORD && kt == KeywordType.USING)
      {
        StringBuilder localBuilder = new StringBuilder();

        string kWord = _tokenizer.GetValue<string>();

        // <directive>
        OpenTag(localBuilder, "directive", depth, inLine:false);

        InLineTags(localBuilder, tt.ToString(), kWord, depth+1);

        AdvanceStreamWithException();

        bool success = CompileIdentifier(localBuilder, depth+1);

        AdvanceStreamWithException();

        success &= CompileSymbol(localBuilder, ';', depth+1);

        if(success == false)
        {
          Console.WriteLine(localBuilder.ToString());
          throw new Exception(Constants.SyntaxError);
        }

        // </directive>
        CloseTag(localBuilder, "directive", depth);

        tagBuilder.Append(localBuilder.ToString());

        localBuilder.Clear();

        return true;
      }
      
      return false;
    }

    private bool CompileNamespaceDeclaration(StringBuilder tagBuilder, int depth)
    {
      TokenType   tt = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      if(tt == TokenType.KEYWORD && kt == KeywordType.NAMESPACE)
      {
        StringBuilder localBuilder = new StringBuilder();

        string ktStr = kt.ToString();
        string kWord = _tokenizer.GetValue<string>();

        // <namespace>
        OpenTag(localBuilder, ktStr, depth, inLine:false);

        InLineTags(localBuilder, tt.ToString(), kWord, depth+1);

        AdvanceStreamWithException();

        bool success = CompileIdentifier(localBuilder, depth+1);

        AdvanceStreamWithException();

        success &= CompileStatementSequence(localBuilder, depth+1);

        if(success == false)
        {
          Console.WriteLine(localBuilder.ToString());
          localBuilder.Clear();
          throw new Exception(Constants.SyntaxError);
        }

        // </namespace>
        CloseTag(localBuilder, ktStr, depth);

        tagBuilder.Append(localBuilder.ToString());

        localBuilder.Clear();

        return true;
      }

      return false;
    }

    private bool CompileClassDeclaration(StringBuilder tagBuilder, int depth)
    {
      // Look ahead to count access and class modifiers in order to find the class keyword and class name identifier
      int  lookAheadCount            = GetAccessModifierCount();
      lookAheadCount                += GetClassModifierCount();
      bool isClassDeclarationSyntax  = KeywordLookAhead(maxIterations:lookAheadCount, LookAheadMatch.ANY, KeywordType.CLASS);
      isClassDeclarationSyntax      &= TokenTypeLookAhead(maxIterations:lookAheadCount+1, LookAheadMatch.ANY, TokenType.IDENTIFIER);

      if(isClassDeclarationSyntax)
      {
        StringBuilder localBuilder = new StringBuilder();
        
        // <class>
        OpenTag(localBuilder, KeywordType.CLASS.ToString(), depth, false);

        /* Class declarations in C# support a mix of access, and class modifiers (e.g. "partial static public", or "static internal")
         * A comprehensive compilation of legal combinations is beyond the scope of this version of the parser. For the time being
         * modifers are gathered in the order they're found before the "class" keyword. */
        TokenType   tt = _tokenizer.GetTokenType();
        KeywordType kt = _tokenizer.GetKeywordType();
        while(IsAccessModifier(kt) || IsClassModifier(kt))
        {
          string kStr = _tokenizer.GetValue<string>();
          InLineTags(localBuilder, tt.ToString(), kStr, depth+1);
          AdvanceStreamWithException();
          kt = _tokenizer.GetKeywordType();
        }

        bool success = CompileKeyWord(localBuilder, KeywordType.CLASS, depth+1);

        AdvanceStreamWithException();

        success &= CompileIdentifier(localBuilder, depth+1);

        AdvanceStreamWithException();

        // Derived type compilation
        if(CompileSymbol(localBuilder, ':', depth+1))
        {
          AdvanceStreamWithException();

          success &= CompileIdentifier(localBuilder, depth+1);

          AdvanceStreamWithException();
        }

        success &= CompileStatementSequence(localBuilder, depth+1);

        if(success == false)
        {
          Console.WriteLine(localBuilder.ToString());
          localBuilder.Clear();
          throw new Exception(Constants.SyntaxError);
        }

        // </class>
        CloseTag(localBuilder, KeywordType.CLASS.ToString(), depth);

        tagBuilder.Append(localBuilder.ToString());

        localBuilder.Clear();

        return true;
      }

      return false;
    }

    private bool CompileClassConstructor(StringBuilder tagBuilder, int depth)
    {
      TokenType tt   = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      // A constructor can have 0 or 1 access modifiers before its declaration.
      int lookAheadCount        = GetAccessModifierCount();
      bool isConstructorSyntax  = TokenTypeLookAhead(maxIterations:lookAheadCount, LookAheadMatch.ANY, TokenType.IDENTIFIER);
      isConstructorSyntax      |= tt == TokenType.IDENTIFIER;
      isConstructorSyntax      &= SymbolLookAhead(maxIterations:lookAheadCount+1, LookAheadMatch.ANY, '(');

      if(isConstructorSyntax)
      {
        StringBuilder localBuilder = new StringBuilder();

        // <constructor>
        OpenTag(localBuilder, "constructor", depth, false);

        // Constructor declarations in C# can support 0 or 1 modifiers
        kt = _tokenizer.GetKeywordType();
        if(IsAccessModifier(kt))
        {
          string kStr = _tokenizer.GetValue<string>();
          InLineTags(localBuilder, tt.ToString(), kStr, depth+1);

          AdvanceStreamWithException();
        }

        bool success = CompileIdentifier(localBuilder, depth+1);

        AdvanceStreamWithException();

        success &= CompileParameterList(localBuilder, depth+1);

        AdvanceStreamWithException();

        // Base constructor look head
        tt = _tokenizer.GetTokenType();

        if(tt == TokenType.SYMBOL)
        {
          char sym = _tokenizer.GetValue<char>();
          if(sym.Equals(':'))
          {
            CompileSymbol(localBuilder, ':', depth+1);

            AdvanceStreamWithException();

            kt = _tokenizer.GetKeywordType();

            success &= CompileKeyWord(localBuilder, kt, depth+1);

            AdvanceStreamWithException();

            success &= CompileParameterList(localBuilder, depth+1);

            AdvanceStreamWithException();
          }
        }

        success &= CompileStatementSequence(localBuilder, depth+1);

        if(success == false)
        {
          Console.WriteLine(localBuilder.ToString());
          localBuilder.Clear();
          throw new Exception(Constants.SyntaxError);
        }

        // </contructor>
        CloseTag(localBuilder, "constructor", depth);

        tagBuilder.Append(localBuilder.ToString());

        localBuilder.Clear();

        return true;
      }

      return false;
    }

    private bool CompileClassVariableDeclaration(StringBuilder tagBuilder, int depth)
    {
      TokenType   tt = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      // Class variables cannot be declared with the var keyword.
      if(tt == TokenType.KEYWORD && kt == KeywordType.VAR)
      {
        return false;
      }

      /* Get the number of any access modifiers that exist
       * Look ahead +1 to to skip over the return type.  
       * Look ahead +2 to get supported class variable symbols that should follow an identifier */
      int  lookAheadCount         = GetAccessModifierCount();
      bool isClassVariableSyntax  = TokenTypeLookAhead(lookAheadCount + 1, LookAheadMatch.ANY, TokenType.IDENTIFIER);
      isClassVariableSyntax      &= SymbolLookAhead(lookAheadCount + 2, LookAheadMatch.ANY, '=', ';');

      if(isClassVariableSyntax)
      {
        StringBuilder localBuilder = new StringBuilder();

        // <variable>
        OpenTag(localBuilder, "variable", depth, false);

        BuildGeneralStatement(localBuilder, depth+1, ';');

        AdvanceStreamWithException();

        CompileSymbol(localBuilder, ';', depth+1);

        // </variable>
        CloseTag(localBuilder, "variable", depth);

        tagBuilder.Append(localBuilder.ToString());

        localBuilder.Clear();

        return true;
      }

      return false;
    }

    private bool CompileLocalVariableDeclaration(StringBuilder tagBuilder, int depth)
    {
      TokenType   tt = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      // Local variable can start with a type identifier followed by an assingment expression, or a built-in keyword.
      bool isLocalVariableSyntax = false;
      if(tt == TokenType.IDENTIFIER)
      {
        isLocalVariableSyntax = SymbolLookAhead(maxIterations:1, LookAheadMatch.ANY, '=');
      }
      else if(tt == TokenType.KEYWORD)
      {
        isLocalVariableSyntax |= (kt == KeywordType.CONST);
        isLocalVariableSyntax |= (kt == KeywordType.VAR);
        isLocalVariableSyntax |= IsBuiltInType(kt);
      }

      if(isLocalVariableSyntax)
      {
        StringBuilder localBuilder = new StringBuilder();

        // <variable>
        OpenTag(localBuilder, "variable", depth+1, false);

        BuildGeneralStatement(localBuilder, depth+1, ';');

        AdvanceStreamWithException();

        CompileSymbol(localBuilder, ';', depth+1);

        // </variable>
        CloseTag(localBuilder, "variable", depth+1);

        tagBuilder.Append(localBuilder.ToString());

        localBuilder.Clear();

        return true;
      }

      return false;
    }

    private bool CompileIdentifier(StringBuilder tagBuilder, int depth)
    {
      TokenType tt = _tokenizer.GetTokenType();

      if(tt == TokenType.IDENTIFIER)
      {
        StringBuilder idBuilder = new StringBuilder();
        BuildIdentifier(idBuilder, TokenType.IDENTIFIER);
        InLineTags(tagBuilder, tt.ToString(), idBuilder.ToString(), depth);
        return true;
      }

      return false;
    }

    private bool CompileFunctionDeclaration(StringBuilder tagBuilder, int depth)
    {
      int lookAheadCount     = GetAccessModifierCount();
      bool isFunctionSyntax  = KeywordLookAhead(maxIterations:lookAheadCount, LookAheadMatch.ANY, KeywordType.VOID);
      isFunctionSyntax      |= TokenTypeLookAhead(maxIterations:lookAheadCount+1, LookAheadMatch.ANY, TokenType.IDENTIFIER);
      isFunctionSyntax      &= SymbolLookAhead(maxIterations:lookAheadCount+2, LookAheadMatch.ANY, '(');

      if (isFunctionSyntax)
      {
        StringBuilder localBuilder = new StringBuilder();

        //<function>
        OpenTag(localBuilder, "function", depth, false);

        TokenType   tt = _tokenizer.GetTokenType();
        KeywordType kt = _tokenizer.GetKeywordType();

        /* Function declarations in C# support mix of keywords and identifiers (e.g. "public override void", or "static private MyType")
         * A comprehensive compilation of legal combinations is beyond the scope of this version of the parser. For the time being
         * keywords are gathered in the order they're found before the function identifier*/
        while(IsAccessModifier(kt) || IsMemberModifier(kt))
        {
          string kStr = _tokenizer.GetValue<string>();
          InLineTags(localBuilder, tt.ToString(), kStr, depth+1);
          AdvanceStreamWithException();
          kt = _tokenizer.GetKeywordType();
        }

        // Return type comes after access and member modifiers
        tt = _tokenizer.GetTokenType();
        if(kt.Equals(KeywordType.VOID) || tt == TokenType.IDENTIFIER)
        {
          string kStr = _tokenizer.GetValue<string>();
          InLineTags(localBuilder, tt.ToString(), kStr, depth+1);
          AdvanceStreamWithException();
        }
        else
        {
          throw new Exception(Constants.SyntaxError);
        }

        bool success = CompileIdentifier(localBuilder, depth+1);

        AdvanceStreamWithException();

        success &= CompileParameterList(localBuilder, depth+1);

        AdvanceStreamWithException();

        success &= CompileStatementSequence(localBuilder, depth+1);

        if(success == false)
        {
          Console.WriteLine(localBuilder.ToString());
          throw new Exception(Constants.SyntaxError);
        }

        // </function>
        CloseTag(localBuilder, "function", depth);

        tagBuilder.Append(localBuilder.ToString());

        localBuilder.Clear();

        return true;
      }

      return false;
    }

    private bool CompileParameterList(StringBuilder tagBuilder, int depth)
    {
      TokenType tt = _tokenizer.GetTokenType();

      if(tt == TokenType.SYMBOL)
      {
        char sym = _tokenizer.GetValue<char>();
        if (sym.Equals('('))
        {
          StringBuilder localBuilder  = new StringBuilder();

          // <parameterList>
          OpenTag(localBuilder, "parameters", depth+1, false);

          bool success = CompileSymbol(localBuilder, '(', depth + 1);

          // Look ahead for non-empty parameter list
          if(_tokenizer.LookAheadTokenType() != TokenType.SYMBOL)
          {

            AdvanceStreamWithException();

            while(_tokenizer.HasMoreTokens())
            {
              // <parameter>
              OpenTag(localBuilder, "parameter", depth+2, false);

              BuildGeneralStatement(localBuilder, depth+3, ',', ')');

              // </parameter>
              CloseTag(localBuilder, "parameter", depth+2);

              tt = _tokenizer.LookAheadTokenType();
              if(tt == TokenType.SYMBOL)
              {
                sym = _tokenizer.LookAheadValue<char>();
                if(sym.Equals(')'))
                {
                  break;
                }
              }

              // Advance once to move onto the termianting comma
              AdvanceStreamWithException();

              CompileSymbol(localBuilder, ',', depth+2);

              // Advance again to move onto the next paramter
              AdvanceStreamWithException();
            }
          }
       
          AdvanceStreamWithException();

          success &= CompileSymbol(localBuilder, ')', depth + 1);

          if(success == false)
          {
            Console.WriteLine(localBuilder.ToString());
            throw new Exception(Constants.SyntaxError);
          }

          //</parameterList>
          CloseTag(localBuilder, "parameters", depth+1);

          tagBuilder.Append(localBuilder.ToString());

          localBuilder.Clear();

          return true;
        }
      }

      return false;
    }

    private void CompileIfStatement()
    {
    }

    private void CompileForLoop()
    {
    }

    private bool CompileFunctionCall(StringBuilder tagBuilder, int depth)
    {
      TokenType tt = _tokenizer.GetTokenType();

      if(tt == TokenType.IDENTIFIER)
      {
        StringBuilder localBuilder = new StringBuilder();
        
        // <functioncall>
        OpenTag(localBuilder, "functionCall", depth+1, false);

        StringBuilder idBuilder = new StringBuilder();
        BuildIdentifier(idBuilder, TokenType.IDENTIFIER);
        InLineTags(localBuilder, TokenType.IDENTIFIER.ToString(), idBuilder.ToString(), depth+2);


        BuildGeneralStatement(localBuilder, depth+2, ';');

        AdvanceStreamWithException();

        CompileSymbol(localBuilder, ';', depth+1);

        // </functioncall>
        CloseTag(localBuilder, "functionCall", depth+1);

        tagBuilder.Append(localBuilder);

        localBuilder.Clear();

        return true;
      }

      return false;
    }

    private bool CompileKeyWord(StringBuilder tagBuilder, KeywordType keywordType, int depth)
    {
      TokenType   tt = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      if(tt == TokenType.KEYWORD && kt == keywordType)
      {
        string kStr = _tokenizer.GetValue<string>();
        InLineTags(tagBuilder, tt.ToString(), kStr, depth);
        return true;
      }

      return false;
    }

    private bool CompileSymbol(StringBuilder tagBuilder, char symbol, int depth)
    {
      TokenType tt = _tokenizer.GetTokenType();
      char      cs = _tokenizer.GetValue<char>();

      if(tt == TokenType.SYMBOL && cs.Equals(symbol))
      {
        InLineTags(tagBuilder, tt.ToString(), cs.ToString(), depth);
        return true;
      }

      return false;
    }

    private bool CompileConstantValue(StringBuilder tagBuilder, int depth)
    {
      TokenType tt = _tokenizer.GetTokenType();
      
      if(tt == TokenType.NUMBER || tt == TokenType.STRING)
      {
        string constValue = _tokenizer.GetValue<string>();
        constValue = constValue.Replace("\"",""); // strip quotes from strings.
        InLineTags(tagBuilder, tt.ToString(), constValue, depth);

        return true;
      }

      return false;
    }

    private void BuildIdentifier(StringBuilder idBuilder, TokenType expectedTokenType)
    {
      if(expectedTokenType == TokenType.IDENTIFIER)
      {
        idBuilder.Append(_tokenizer.GetValue<string>());
        if(_tokenizer.LookAheadTokenType() == TokenType.SYMBOL)
        {
          char c = _tokenizer.LookAheadValue<char>();
          if(c.Equals('.'))
          {
            _tokenizer.Advance();
            BuildIdentifier(idBuilder, TokenType.SYMBOL);
          }
        }
      }
      else if(expectedTokenType == TokenType.SYMBOL)
      {
        idBuilder.Append(_tokenizer.GetValue<char>());
        if(_tokenizer.LookAheadTokenType() == TokenType.IDENTIFIER)
        {
          _tokenizer.Advance();
          BuildIdentifier(idBuilder, TokenType.IDENTIFIER);
        }
      }
      else
      {
        return;
      }
    }

    private void BuildGeneralStatement(StringBuilder tagBuilder, int depth, params char[] terminators)
    {
      TokenType tt = _tokenizer.GetTokenType();
      
      switch(tt)
      {
        case TokenType.KEYWORD:
          KeywordType kt = _tokenizer.GetKeywordType();
          CompileKeyWord(tagBuilder, kt, depth);
          break;
        case TokenType.IDENTIFIER:
          CompileIdentifier(tagBuilder, depth);
          break;
        case TokenType.SYMBOL:
          char sym = _tokenizer.GetValue<char>();
          CompileSymbol(tagBuilder, sym, depth);
          break;
        case TokenType.NUMBER:
        case TokenType.STRING:
          CompileConstantValue(tagBuilder, depth);
          break;
      }

      if(_tokenizer.LookAheadTokenType() == TokenType.SYMBOL)
      {
        char nextSym = _tokenizer.LookAheadValue<char>();
        for(int i=0; i<terminators.Length; i++)
        {
          if (nextSym.Equals(terminators[i])) return;
        }
      }

      AdvanceStreamWithException();

      BuildGeneralStatement(tagBuilder, depth, terminators);
    }

    private bool TokenTypeLookAhead(int maxIterations, LookAheadMatch matchType, params TokenType[] tokeTypes)
    {
      int matchCount = 0;

      for(int i=0; i<tokeTypes.Length; i++)
      {
        TokenType searchTt = tokeTypes[i];

        for(int j=0; j<maxIterations; j++)
        {
          if(_tokenizer.HasTokenAt(j) == false) break;

          TokenType lookAheadTt = _tokenizer.LookAheadTokenType(j);

          if(searchTt == lookAheadTt)
          {
            switch(matchType)
            {
              case LookAheadMatch.ANY:
                return true;
              case LookAheadMatch.ALL:
                if(++matchCount == tokeTypes.Length)
                {
                  return true;
                }
                break;
            }
          }
        }
      }

      return false;
    }

    private bool KeywordLookAhead(int maxIterations, LookAheadMatch matchType, params KeywordType[] keywordTypes)
    {
      int matchCount = 0;

      for(int i=0; i<keywordTypes.Length; i++)
      {
        KeywordType searchKw = keywordTypes[i];

        for(int j=0; j<maxIterations; j++)
        {
          if(_tokenizer.HasTokenAt(j) == false) break;

          KeywordType lookAheadKw = _tokenizer.LookAheadKeywordType(j);

          if(searchKw == lookAheadKw)
          {
            switch(matchType)
            {
              case LookAheadMatch.ANY:
                return true;
              case LookAheadMatch.ALL:
                if(++matchCount == keywordTypes.Length)
                {
                  return true;
                }
                break;
            }
          }
        }
      }

      return false;
    }

    private bool SymbolLookAhead(int maxIterations, LookAheadMatch matchType, params char[] symbols)
    {
      int matchCount = 0;

      for(int i=0; i<symbols.Length; i++)
      {
        char searchSym = symbols[i];

        for(int j=0; j<maxIterations; j++)
        {
          if(_tokenizer.HasTokenAt(j) == false) break;
          
          TokenType tt = _tokenizer.LookAheadTokenType(j);

          if(tt != TokenType.SYMBOL) continue;

          char lookAheadSym = _tokenizer.LookAheadValue<char>(j);

          if(searchSym == lookAheadSym)
          {
            switch(matchType)
            {
              case LookAheadMatch.ANY:
                return true;
              case LookAheadMatch.ALL:
                if(++matchCount == symbols.Length)
                {
                  return true;
                }
                break;
            }
          }
        }
      }

      return false;
    }

    private int GetAccessModifierCount()
    {
      TokenType   tt = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      int matchCount = 0;

      if(IsAccessModifier(kt) || IsMemberModifier(kt)) matchCount++;

      int index = 0;
      while(_tokenizer.HasTokenAt(index))
      {
        tt = _tokenizer.LookAheadTokenType(index);
        kt = _tokenizer.LookAheadKeywordType(index);

        if(IsAccessModifier(kt) || IsMemberModifier(kt))
        {
          matchCount++;
        }
        else if(tt == TokenType.IDENTIFIER)
        {
          break;
        }

        index++;
      }

      return matchCount;
    }

    private int GetClassModifierCount()
    {
      TokenType   tt = _tokenizer.GetTokenType();
      KeywordType kt = _tokenizer.GetKeywordType();

      int matchCount = 0;

      if(IsClassModifier(kt)) matchCount++;

      int index = 0;
      while(_tokenizer.HasTokenAt(index))
      {
        kt = _tokenizer.LookAheadKeywordType(index);

        if(IsClassModifier(kt))
        {
          matchCount++;
        }
        else if (tt == TokenType.IDENTIFIER)
        {
          break;
        }

        index++;
      }

      return matchCount;
    }

    private bool IsAccessModifier(KeywordType kt)
    {
      return
        kt == KeywordType.PUBLIC    ||
        kt == KeywordType.PRIVATE   ||
        kt == KeywordType.INTERNAL  ||
        kt == KeywordType.PROTECTED ||
        kt == KeywordType.CONST     ||
        kt == KeywordType.STATIC    ||
        kt == KeywordType.SEALED;
    }

    private bool IsClassModifier(KeywordType kt)
    {
      return
        kt == KeywordType.PARTIAL ||
        kt == KeywordType.ABSTRACT;
    }

    private bool IsMemberModifier(KeywordType kt)
    {
      return
        kt == KeywordType.ABSTRACT ||
        kt == KeywordType.VIRTUAL  ||
        kt == KeywordType.OVERRIDE;
    }

    private bool IsBuiltInType(KeywordType kt)
    {
      return
        kt == KeywordType.SBYTE   ||
        kt == KeywordType.BYTE    ||
        kt == KeywordType.SHORT   ||
        kt == KeywordType.USHORT  ||
        kt == KeywordType.INT     ||
        kt == KeywordType.UINT    ||
        kt == KeywordType.LONG    ||
        kt == KeywordType.ULONG   ||
        kt == KeywordType.CHAR    ||
        kt == KeywordType.STRING  ||
        kt == KeywordType.FLOAT   ||
        kt == KeywordType.DOUBLE  ||
        kt == KeywordType.BOOL    ||
        kt == KeywordType.DECIMAL ||
        kt == KeywordType.OBJECT;
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

