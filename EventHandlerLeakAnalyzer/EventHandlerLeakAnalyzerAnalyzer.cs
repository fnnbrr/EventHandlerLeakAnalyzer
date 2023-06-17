using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace EventHandlerLeakAnalyzer
{
    /// <summary>
    /// Used to detect memory leaks from event handlers that do not unsubscribe.
    /// See: https://www.jetbrains.com/help/dotmemory/Inspections.html#event_handlers_leak
    /// See: https://en.wikipedia.org/wiki/Lapsed_listener_problem
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EventHandlerLeakAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "EventHandlerLeakAnalyzer";
        
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            id: DiagnosticId,
            title: "EventHandlerLeakAnalyzer",
            messageFormat: "Message format for {0}",
            category: "Naming",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Description here");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            
            context.RegisterSyntaxNodeAction(AnalyzeEventSubscriptions, SyntaxKind.AddAssignmentExpression);
        }
        
        private static void AnalyzeEventSubscriptions(SyntaxNodeAnalysisContext context)
        {
            AssignmentExpressionSyntax assignmentExpression = (AssignmentExpressionSyntax)context.Node;

            // Check if the assigned symbol is an event subscription
            if (assignmentExpression.Left is MemberAccessExpressionSyntax identifier &&
                assignmentExpression.OperatorToken.Kind() == SyntaxKind.PlusEqualsToken &&
                context.SemanticModel.GetSymbolInfo(identifier).Symbol is IEventSymbol eventSymbol)
            {
                if (!ContainsUnsubscription(context, assignmentExpression, eventSymbol))
                {
                    // Report a diagnostic if the event is not explicitly unsubscribed
                    Diagnostic diagnostic = Diagnostic.Create(Rule, assignmentExpression.GetLocation());
                    
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
        
        private static bool ContainsUnsubscription(SyntaxNodeAnalysisContext context, 
            SyntaxNode subscribeAssignmentExpression, ISymbol subscribeEventSymbol)
        {
            ISymbol containingType = context.ContainingSymbol.ContainingType;
            SemanticModel semanticModel = context.SemanticModel;
            
            foreach (SyntaxReference syntaxReference in containingType.DeclaringSyntaxReferences)
            {
                foreach (SyntaxNode syntaxNode in syntaxReference.GetSyntax().DescendantNodesAndSelf())
                {
                    if (!(syntaxNode is AssignmentExpressionSyntax assignmentExpression)) continue;
                    
                    if (!(assignmentExpression.Left is MemberAccessExpressionSyntax identifier)) continue;
                    
                    if (assignmentExpression.OperatorToken.Kind() != SyntaxKind.MinusEqualsToken) continue;
                    
                    if (!(semanticModel.GetSymbolInfo(identifier).Symbol is IEventSymbol eventSymbol)) continue;
                    
                    if (!SymbolEqualityComparer.Default.Equals(eventSymbol, subscribeEventSymbol)) continue;
                    
                    // TODO: check if the delegate on the right side is the same in sub and unsub
                    
                    return true;
                }
            }
            
            return false;
        }
    }
}
