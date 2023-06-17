using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = EventHandlerLeakAnalyzer.Test.CSharpCodeFixVerifier<
    EventHandlerLeakAnalyzer.EventHandlerLeakAnalyzerAnalyzer,
    EventHandlerLeakAnalyzer.EventHandlerLeakAnalyzerCodeFixProvider>;

namespace EventHandlerLeakAnalyzer.Test
{
    [TestClass]
    public class EventHandlerLeakAnalyzerUnitTest
    {
        // Shouldn't trigger a diagnostic (empty code)
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        
        // Should trigger one diagnostic
        [TestMethod]
        public async Task TestMethod2()
        {
            string test = await File.ReadAllTextAsync("../../../TestFiles/TestCode2.cs");
            
            DiagnosticResult expected = VerifyCS.Diagnostic(EventHandlerLeakAnalyzerAnalyzer.DiagnosticId).WithLocation(15, 13);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
        
        // Shouldn't trigger a diagnostic (event unsubscribed from)
        [TestMethod]
        public async Task TestMethod3()
        {
            string test = await File.ReadAllTextAsync("../../../TestFiles/TestCode3.cs");
            
            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        
        // Should trigger one diagnostic
        [TestMethod]
        public async Task TestMethod4()
        {
            string test = await File.ReadAllTextAsync("../../../TestFiles/TestCode4.cs");
            
            DiagnosticResult expected = VerifyCS.Diagnostic(EventHandlerLeakAnalyzerAnalyzer.DiagnosticId).WithLocation(21, 13);
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        // TODO: work of this example to make a codefix
        //Diagnostic and CodeFix both triggered and checked for
        // [TestMethod]
        public async Task ExampleTest()
        {
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";

            var expected = VerifyCS.Diagnostic("EventHandlerLeakAnalyzer").WithLocation(0).WithArguments("TypeName");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
