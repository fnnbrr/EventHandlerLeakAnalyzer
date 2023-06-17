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
            messageFormat: "Event subscribed to but never unsubscribed from",
            category: "Memory Leak",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Detects events that are never unsubscribed from");

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
            AssignmentExpressionSyntax subscribeAssignmentExpression, ISymbol subscribeEventSymbol)
        {
            ISymbol containingType = context.ContainingSymbol.ContainingType;
            SemanticModel semanticModel = context.SemanticModel;
            
            foreach (SyntaxReference syntaxReference in containingType.DeclaringSyntaxReferences)
            {
                foreach (SyntaxNode syntaxNode in syntaxReference.GetSyntax().DescendantNodesAndSelf())
                {
                    // Check that this syntax node represents a -= assignment on an event class member
                    if (!(syntaxNode is AssignmentExpressionSyntax assignmentExpression &&
                        assignmentExpression.Left is MemberAccessExpressionSyntax identifier &&
                        assignmentExpression.OperatorToken.Kind() == SyntaxKind.MinusEqualsToken &&
                        semanticModel.GetSymbolInfo(identifier).Symbol is IEventSymbol eventSymbol)) continue;
                    
                    // Check that the sub and unsub event members are the same
                    if (!SymbolEqualityComparer.Default.Equals(eventSymbol, subscribeEventSymbol)) continue;
                    
                    // Check that the sub and unsub event responses are the same
                    ISymbol subResponseSymbol = semanticModel.GetSymbolInfo(subscribeAssignmentExpression.Right).Symbol;
                    ISymbol unsubResponseSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Right).Symbol;
                    if (!SymbolEqualityComparer.Default.Equals(subResponseSymbol, unsubResponseSymbol)) continue;
                    
                    return true;
                }
            }
            
            return false;
        }
    }
}
