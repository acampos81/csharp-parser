using System.Threading.Tasks;

namespace CSharpParser
{
  public interface ICompiler
  {
    bool CompileStatements();
    bool CompileStatementSequence();
    bool CompileUsingDirective();
    bool CompileNamespaceDeclaration();
    bool CompileClassDeclaration();
    bool CompileClassConstructor();
    bool CompileVariableDeclaration();
    bool CompileFunctionDeclaration();
    bool CompileFunctionBody();
    bool CompileParameterList();
    bool CompileIfStatement();
    bool CompileSwitchStatement();
    bool CompileForStatement();
    bool CompileForEachStatement();
    bool CompileWhileStatement();
    bool CompileDoStatement();
    bool CompileTryStatement();
    bool CompileExpression();
    bool CompileReturn();
  }
}
